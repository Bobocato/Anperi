using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Api;
using JJA.Anperi.Api.Shared;
using JJA.Anperi.HostApi;
using JJA.Anperi.PeripheralApi;
using JJA.Anperi.Server.Model;
using JJA.Anperi.Server.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SQLitePCL;

namespace JJA.Anperi.Server
{
    public class AuthenticatedWebSocketConnection
    {
        private readonly HttpContext _context;
        private readonly WebSocket _socket;
        private readonly byte[] _buffer;
        private readonly RegisteredDevice _device;
        private readonly ILogger<AnperiWebSocketMiddleware> _logger;
        private readonly AnperiWebSocketMiddleware _anperiManager;
        private readonly AnperiDbContext _db;
        private AuthenticatedWebSocketConnection _partner;
        private readonly object _syncRootPartner = new object();

        public AuthenticatedWebSocketConnection(HttpContext context, WebSocket socket, byte[] buffer, RegisteredDevice device, AnperiDbContext dbContext, ILogger<AnperiWebSocketMiddleware> logger, AnperiWebSocketMiddleware anperiManager)
        {
            _context = context;
            _socket = socket;
            _buffer = buffer;
            _device = device;
            _logger = logger;
            _anperiManager = anperiManager;
            _db = dbContext;
            _anperiManager.PeripheralLoggedIn += _anperiManager_PeripheralLoggedIn;
            _anperiManager.PeripheralLoggedOut += _anperiManager_PeripheralLoggedOut;
        }

        private async void _anperiManager_PeripheralLoggedOut(object sender, AuthenticatedWebSocketEventArgs e)
        {
            if (e.Connection.Device.Id != _partner.Device.Id)
            {
                await _socket.SendJson(
                    HostJsonApiObjectFactory.CreatePairedPeripheralLoggedOffMessage(e.Connection.Device.Id));
            }
            else
            {
                lock (_syncRootPartner)
                {
                    _partner = null;
                }
                await _socket.SendJson(SharedJsonApiObjectFactory.CreatePartnerDisconnected());
            }
            
        }

        private async void _anperiManager_PeripheralLoggedIn(object sender, AuthenticatedWebSocketEventArgs e)
        {
            await _socket.SendJson(
                HostJsonApiObjectFactory.CreatePairedPeripheralLoggedOnMessage(e.Connection.Device.Id));
        }

        public bool IsPeripheral => Device is Peripheral;

        public RegisteredDevice Device => _device;

        public HttpContext Context
        {
            get { return _context; }
        }

        private async void PartnerCloseConnection()
        {
            lock (_syncRootPartner)
            {
                _partner = null;
            }
            await _socket.SendJson(SharedJsonApiObjectFactory.CreatePartnerDisconnected());
        }

        private bool PartnerConnect(AuthenticatedWebSocketConnection connection)
        {
            lock (_syncRootPartner)
            {
                if (_partner == null)
                {
                    _partner = connection;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private async void PartnerSendMessage(JsonApiObject msg)
        {
            await _socket.SendJson(msg);
        }

        public async Task<WebSocketCloseStatus> Run(CancellationToken token)
        {
            WebSocketApiResult apiObjectResult =
                await _socket.ReceiveApiMessage(new ArraySegment<byte>(_buffer), token);
            while (!apiObjectResult.SocketResult.CloseStatus.HasValue && !token.IsCancellationRequested)
            {
                if (apiObjectResult.Obj == null)
                {
                    await _socket.SendJson(
                        SharedJsonApiObjectFactory.CreateError(apiObjectResult.JsonException.Message), token);
                }
                else
                {
                    switch (apiObjectResult.Obj.context)
                    {
                        case JsonApiContextTypes.server:
                            if (!(await HandleSharedMessage(apiObjectResult.Obj, token)))
                            {
                                switch (Device)
                                {
                                    case Host _:
                                        await HandleHostMessage(apiObjectResult.Obj, token);
                                        break;
                                    case Peripheral _:
                                        await HandlePeripheralMessage(apiObjectResult.Obj, token);
                                        break;
                                }
                            }
                            break;
                        case JsonApiContextTypes.device:
                            if (_partner != null)
                            {
                                _partner?.PartnerSendMessage(apiObjectResult.Obj);
                            }
                            else
                            {
                                await _socket.SendJson(
                                    SharedJsonApiObjectFactory.CreateError(
                                        "You don't have a partner, annoy somebody else >.>"));
                            }
                            break;
                    }
                }

                apiObjectResult = await _socket.ReceiveApiMessage(new ArraySegment<byte>(_buffer), token);
            }
            return apiObjectResult.SocketResult.CloseStatus ?? WebSocketCloseStatus.NormalClosure;
        }

        private async Task<bool> HandleSharedMessage(JsonApiObject message, CancellationToken token)
        {
            SharedJsonRequestCode msgCode;
            try
            {
                msgCode = Enum.Parse<SharedJsonRequestCode>(message.message_code);
            }
            catch (Exception)
            {
                return false;
            }
            switch (msgCode)
            {
                case SharedJsonRequestCode.login:
                case SharedJsonRequestCode.register:
                    await _socket.SendJson(
                        SharedJsonApiObjectFactory.CreateError("You can't login while you're logged in ..."), token);
                    break;
                case SharedJsonRequestCode.set_own_name:
                    if (message.data.TryGetValue("name", out string newName))
                    {
                        _db.Hosts.Find(_device.Id).Name = newName;
                        _db.SaveChanges();
                        await _socket.SendJson(SharedJsonApiObjectFactory.CreateChangeOwnNameResponse(true, newName));
                    }
                    else
                    {
                        await _socket.SendJson(SharedJsonApiObjectFactory.CreateError("name not defined"));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        private async Task HandlePeripheralMessage(JsonApiObject message, CancellationToken token)
        {
            PeripheralRequestCode msgCode;
            try
            {
                msgCode = Enum.Parse<PeripheralRequestCode>(message.message_code);
            }
            catch (Exception)
            {
                await _socket.SendJson(SharedJsonApiObjectFactory.CreateError($"{message.message_code} is not a valid message code (or I forgot it)."),
                    token);
                return;
            }
            switch (msgCode)
            {
                case PeripheralRequestCode.get_pairing_code:
                    string pairingCode;
                    ActivePairingCode dbCode =
                        _db.ActivePairingCodes.SingleOrDefault(c => c.PeripheralId == Device.Id);
                    if (dbCode != null)
                    {
                        pairingCode = dbCode.Code;
                    }
                    else
                    {
                        pairingCode = Cryptography.CreatePairingCode();
                        var codeEntry = new ActivePairingCode
                        {
                            Code = pairingCode,
                            PeripheralId = Device.Id
                        };
                        _db.ActivePairingCodes.Add(codeEntry);
                        _db.SaveChanges();
                    }
                    await _socket.SendJson(PeripheralJsonApiObjectFactory.CreateGetPairingCodeResponse(pairingCode),
                        token); 
                    break;
                default:
                    await _socket.SendJson(SharedJsonApiObjectFactory.CreateError($"Function {msgCode.ToString()} not implemented yet."),
                        token);
                    _logger.LogWarning($"PeripheralRequestCode.{msgCode.ToString()} not implemented.");
                    break;
            }
        }

        private async Task HandleHostMessage(JsonApiObject message, CancellationToken token)
        {
            HostRequestCode msgCode;
            try
            {
                msgCode = Enum.Parse<HostRequestCode>(message.message_code);
            }
            catch (Exception)
            {
                await _socket.SendJson(SharedJsonApiObjectFactory.CreateError($"{message.message_code} is not a valid message code (or I forgot it)."),
                    token);
                return;
            }
            switch (msgCode)
            {
                case HostRequestCode.pair:
                    if (!message.data.TryGetValue("code", out string code))
                    {
                        await _socket.SendJson(
                            SharedJsonApiObjectFactory.CreateError("Parameter code not set or null."), token);
                        return;
                    }
                    try
                    {
                        ActivePairingCode pairingCode = _db.ActivePairingCodes.SingleOrDefault(p => p.Code.Equals(code));
                        if (pairingCode == null)
                        {
                            await _socket.SendJson(
                                SharedJsonApiObjectFactory.CreateError("Pairing code was not valid."), token);
                            return;
                        }
                        Peripheral deviceToPair = _db.Peripherals.Find(pairingCode.PeripheralId);
                        if (deviceToPair != null)
                            deviceToPair.PairedHosts.Add(new HostPeripheral
                            {
                                HostId = Device.Id,
                                PeripheralId = deviceToPair.Id
                            });
                        else
                        {
                            _db.Remove(pairingCode);
                            await _socket.SendJson(
                                SharedJsonApiObjectFactory.CreateError(
                                    "The device you want to pair isn't known to me :("));
                            return;
                        }
                        _db.SaveChanges();
                        await _socket.SendJson(HostJsonApiObjectFactory.CreatePairingResponse(true));
                    }
                    catch (Exception e)
                    {
                        await _socket.SendJson(
                            SharedJsonApiObjectFactory.CreateError($"Internal error handling this request: {e.GetType()} - {e.Message}"), token);
                    }
                    break;
                case HostRequestCode.unpair:
                    if (message.data.TryGetValue("id", out int peripheralId))
                    {
                        HostPeripheral connection = _db.Peripherals.Find(peripheralId)?.PairedHosts
                            .SingleOrDefault(p => p.HostId == Device.Id);
                        if (connection != null)
                        {
                            try
                            {
                                _db.Remove(connection);
                                _db.SaveChanges();
                                await _socket.SendJson(
                                    HostJsonApiObjectFactory.CreateUnpairFromPeripheralResponse(true));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error unpairing devices.");
                                await _socket.SendJson(
                                    HostJsonApiObjectFactory.CreateUnpairFromPeripheralResponse(false));
                            }
                        }
                    }
                    else
                    {
                        await _socket.SendJson(SharedJsonApiObjectFactory.CreateError("id not defined"));
                    }
                    break;
                case HostRequestCode.get_available_peripherals:
                    IEnumerable<Peripheral> peripherals = _db.HostPeripherals.Where(hp => hp.HostId == _device.Id).Select(p => p.Peripheral);
                    await _socket.SendJson(
                        HostJsonApiObjectFactory.CreateAvailablePeripheralResponse(peripherals.Select(p =>
                            new HostJsonApiObjectFactory.ApiPeripheral
                            {
                                id = p.Id,
                                name = p.Name
                            })
                        ), token);
                    break;
                case HostRequestCode.connect_to_peripheral:
                    if (message.data.TryGetValue("id", out int id))
                    {
                        AuthenticatedWebSocketConnection conn = _anperiManager.GetConnectionForId(id);
                        if (conn != null)
                        {
                            _partner = conn;
                            conn.PartnerConnect(this);
                            await _socket.SendJson(HostJsonApiObjectFactory.CreateConnectToPeripheralResponse(true));
                        }
                        else
                        {
                            await _socket.SendJson(HostJsonApiObjectFactory.CreateConnectToPeripheralResponse(false));
                        }
                    }
                    else
                    {
                        await _socket.SendJson(HostJsonApiObjectFactory.CreateConnectToPeripheralResponse(false));
                    }
                    break;
                case HostRequestCode.disconnect_from_peripheral:
                    _partner?.PartnerCloseConnection();
                    _partner = null;
                    await _socket.SendJson(HostJsonApiObjectFactory.CreateDisconnectFromPeripheralResponse(true), token);
                    break;
                case HostRequestCode.change_peripheral_name:
                    if (message.data.TryGetValue("name", out string newName) && message.data.TryGetValue("id", out int periId))
                    {
                        Peripheral p = _db.Peripherals.Find(periId);
                        if (p != null)
                        {
                            p.Name = newName;
                            _db.SaveChanges();
                            await _socket.SendJson(
                                SharedJsonApiObjectFactory.CreateChangeOwnNameResponse(true, newName));
                        }
                        else await _socket.SendJson(
                            SharedJsonApiObjectFactory.CreateChangeOwnNameResponse(false, null));

                    }
                    else
                    {
                        await _socket.SendJson(SharedJsonApiObjectFactory.CreateError("name or id not defined"));
                    }
                    break;
                default:
                    await _socket.SendJson(SharedJsonApiObjectFactory.CreateError($"Function {msgCode.ToString()} not implemented yet."),
                        token);
                    _logger.LogWarning($"HostRequestCode.{msgCode.ToString()} not implemented.");
                    break;
            }
        }
    }
}