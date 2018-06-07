# Anperi (Projekt-SS18)
## how to build C# project:
* build only possible on windows
* only on english or german versions (build events might break for others)
* only tested in VS2017
* required target frameworks: .net framework 4.6.2, .net core 2.0

*before [this commit](https://github.com/Bobocato/Projekt-SS18/commit/4d9b1811ccae0501cb833d1fb1b2480c0ac1a5ee) you need to add /src/cs/nugetOut/ as a nuget repo manually*

After the first build you'll see references that aren't resolved, just build a second time (since the nuget packages exist now the restore should find them and import properly.
