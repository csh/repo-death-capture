﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace SteamApiPatcher;

internal static class Native
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr LoadLibrary(string libname);
}

public class Patcher
{
    private const string GUID = "com.smrkn.repo.steam-api-patcher";
    private const string NAME = "Steam API Patcher";
    private const string VERSION = "0.1.0";

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Facepunch.Steamworks.Win64.dll" };

    internal static ManualLogSource Logger;

    private static Stream? _patched;

    static Patcher()
    {
        Logger = BepInEx.Logging.Logger.CreateLogSource(NAME);
        Logger.LogInfo($"Initializing {NAME} v{VERSION}");
    }

    public static void Initialize()
    {
        // var existingSteamDll = Native.GetModuleHandle("steam_api64.dll");
        // if (existingSteamDll != IntPtr.Zero)
        // {
        //     Logger.LogInfo("steam_api64.dll is already loaded, attempting to free it.");
        //     if (!Native.FreeLibrary(existingSteamDll))
        //     {
        //         Logger.LogError("Failed to free steam_api64.dll");
        //     }
        // }

        // string steamDllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "steam_api64.dll");

        // Logger.LogInfo("Attempting to patch steam_api64.dll");
        // var steamDll = Native.LoadLibrary(steamDllPath);
        // if (steamDll == IntPtr.Zero)
        // {
        //     Logger.LogError("Failed to load steam_api64.dll");
        // }
    }

    public static void Patch(ref AssemblyDefinition assembly)
    {
        Logger.LogInfo("Attempting to patch Facepunch.Steamworks.Win64.dll");

        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Facepunch.Steamworks.Win64.dll");
        _patched = File.Open(path, FileMode.Open, FileAccess.Read);

        AssemblyDefinition replacement = AssemblyDefinition.ReadAssembly(_patched);

        PatchConnectionManager(replacement);
        PatchSocketManager(replacement);
        PatchSteamUser(replacement);
        PatchConnection(replacement);

        replacement.Name = assembly.Name;
        assembly = replacement;
        Logger.LogInfo("Facepunch.Steamworks.Win64.dll patched successfully.");
    }

    private static void PatchConnection(AssemblyDefinition assembly)
    {
        var connection = assembly.MainModule.GetType("Steamworks.Data.Connection");

        if (connection == null)
        {
            Logger.LogError("Failed to find Steamworks.Data.Connection type in assembly.");
            return;
        }

        var originalSendMessage = connection.Methods.First(m =>
            m.Name == "SendMessage"
            && m.Parameters.Count == 3
            && m.Parameters[0].ParameterType.FullName == "System.Byte[]"
            && m.Parameters[1].ParameterType.FullName == "Steamworks.Data.SendType"
            && m.Parameters[2].ParameterType.FullName == "System.UInt16"
        );

        if (originalSendMessage != null)
        {
            Logger.LogInfo("Patching Connection.SendMessage method");

            var sendMessage = new MethodDefinition(
                "SendMessage",
                MethodAttributes.Public | MethodAttributes.HideBySig,
                assembly.MainModule.ImportReference(originalSendMessage.ReturnType)
            );

            // sendMessage.ImplAttributes |= Mono.Cecil.MethodImplAttributes.Unmanaged;

            var byteArrayType = originalSendMessage.Parameters[0].ParameterType;
            var sendType = originalSendMessage.Parameters[1].ParameterType;

            sendMessage.Parameters.Add(new ParameterDefinition("data", ParameterAttributes.None, assembly.MainModule.ImportReference(byteArrayType)));
            sendMessage.Parameters.Add(new ParameterDefinition("sendType", ParameterAttributes.None, assembly.MainModule.ImportReference(sendType)));

            var il = sendMessage.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_U2);
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(originalSendMessage));
            il.Emit(OpCodes.Ret);

            sendMessage.Body.MaxStackSize = 3;
            connection.Methods.Add(sendMessage);
        }
    }

    private static void PatchSteamUser(AssemblyDefinition assembly)
    {
        var steamClient = assembly.MainModule.GetType("Steamworks.SteamClient");
        var steamUser = assembly.MainModule.GetType("Steamworks.SteamUser");

        var steamIdProperty = steamClient.Properties.First(p => p.Name == "SteamId");
        var getSteamId = steamIdProperty.GetMethod;

        var originalGetAuthSessionTicket = steamUser.Methods.First(m => m.Name == "GetAuthSessionTicket" && m.Parameters.Count == 1);

        if (originalGetAuthSessionTicket != null)
        {
            Logger.LogInfo("Patching SteamUser.GetAuthSessionTicket method");

            var getAuthSessionTicket = new MethodDefinition(
                "GetAuthSessionTicket",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                assembly.MainModule.GetType("Steamworks.AuthTicket")
            );

            var il = getAuthSessionTicket.Body.GetILProcessor();
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getSteamId));
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(originalGetAuthSessionTicket));
            il.Emit(OpCodes.Ret);

            steamUser.Methods.Add(getAuthSessionTicket);
        }
    }

    private static void PatchConnectionManager(AssemblyDefinition assembly)
    {
        var connectionManager = assembly.MainModule.GetType("Steamworks.ConnectionManager");

        var originalReceive = connectionManager.Methods.First(m => m.Name == "Receive" && m.Parameters.Count == 2);

        if (originalReceive != null)
        {
            Logger.LogInfo("Patching ConnectionManager.Receive method");

            var receive = new MethodDefinition(
                "Receive",
                MethodAttributes.Public | MethodAttributes.HideBySig,
                assembly.MainModule.TypeSystem.Void
            );

            receive.Parameters.Add(new ParameterDefinition("bufferSize", ParameterAttributes.None, assembly.MainModule.TypeSystem.Int32));

            var il = receive.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(originalReceive));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);

            connectionManager.Methods.Add(receive);
        }

        var originalClose = connectionManager.Methods.First(m =>
            m.Name == "Close" &&
            m.Parameters.Count == 3 &&
            m.Parameters[1].ParameterType.FullName == "System.Int32" &&
            m.Parameters[2].ParameterType.FullName == "System.String"
        );

        if (originalClose != null)
        {
            Logger.LogInfo("Patching ConnectionManager.Close method");

            var close = new MethodDefinition(
                "Close",
                MethodAttributes.Public | MethodAttributes.HideBySig,
                assembly.MainModule.TypeSystem.Void
            );

            var il = close.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldstr, "Closing connection");
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(originalClose));
            il.Emit(OpCodes.Ret);

            close.Body.MaxStackSize = 4;
            connectionManager.Methods.Add(close);
        }
    }

    private static void PatchSocketManager(AssemblyDefinition assembly)
    {
        var socketManager = assembly.MainModule.GetType("Steamworks.SocketManager");

        var originalReceive = socketManager.Methods.First(m => m.Name == "Receive" && m.Parameters.Count == 2);

        if (originalReceive != null)
        {
            Logger.LogInfo("Patching SocketManager.Receive method");

            var receive = new MethodDefinition(
                "Receive",
                MethodAttributes.Public | MethodAttributes.HideBySig,
                assembly.MainModule.TypeSystem.Void
            );

            receive.Parameters.Add(new ParameterDefinition("bufferSize", ParameterAttributes.None, assembly.MainModule.TypeSystem.Int32));

            var il = receive.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(originalReceive));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);

            socketManager.Methods.Add(receive);
        }
    }

    public static void Finish()
    {
        _patched?.Dispose();
    }
}
