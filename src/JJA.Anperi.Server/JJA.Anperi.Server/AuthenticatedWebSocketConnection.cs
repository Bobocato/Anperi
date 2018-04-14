﻿using System;
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
        private readonly AnperiDbContext _db;

        public AuthenticatedWebSocketConnection(HttpContext context, WebSocket socket, byte[] buffer, RegisteredDevice device, AnperiDbContext dbContext, ILogger<AnperiWebSocketMiddleware> logger)
        {
            _context = context;
            _socket = socket;
            _buffer = buffer;
            _device = device;
            _logger = logger;
            _db = dbContext;
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
                    if (_device is Host)
                    {
                        await HandleHostMessage(apiObjectResult.Obj, token);
                    }
                    else if (_device is Peripheral)
                    {
                        await HandlePeripheralMessage(apiObjectResult.Obj, token);
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
                default:
                    await _socket.SendJson(SharedJsonApiObjectFactory.CreateError("Function not implemented yet."),
                        token);
                    _logger.LogWarning($"SharedJsonRequestCode.{msgCode.ToString()} not implemented.");
                    break;
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
                        _db.ActivePairingCodes.SingleOrDefault(c => c.PeripheralId == _device.Id);
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
                            PeripheralId = _device.Id
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
                    if (!message.data.TryGetValue("code", out dynamic codeDyn))
                    {
                        await _socket.SendJson(
                            SharedJsonApiObjectFactory.CreateError("Parameter code not set or null."), token);
                        return;
                    }
                    try
                    {
                        string code = codeDyn;
                        ActivePairingCode pairingCode = _db.ActivePairingCodes.SingleOrDefault(p => p.Code.Equals(code));
                        if (pairingCode == null)
                        {
                            await _socket.SendJson(
                                SharedJsonApiObjectFactory.CreateError("Pairing code was not valid."), token);
                            return;
                        }
                        else
                        {
                            await _socket.SendJson(SharedJsonApiObjectFactory.CreateError($"Pairing request was correct but pairing is not implemented yet :( You would've gotten paired to device {pairingCode.PeripheralId}"),
                                token);
                        }
                    }
                    catch (Exception e)
                    {
                        await _socket.SendJson(
                            SharedJsonApiObjectFactory.CreateError($"Internal error handling this request: {e.GetType()} - {e.Message}"), token);
                    }
                    break;
                case HostRequestCode.unpair:
                case HostRequestCode.get_available_peripherals:
                case HostRequestCode.connect_to_peripheral:
                case HostRequestCode.disconnect_from_peripheral:
                default:
                    await _socket.SendJson(SharedJsonApiObjectFactory.CreateError($"Function {msgCode.ToString()} not implemented yet."),
                        token);
                    _logger.LogWarning($"HostRequestCode.{msgCode.ToString()} not implemented.");
                    break;
            }
        }
    }
}