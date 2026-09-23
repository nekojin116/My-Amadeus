using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

/// <summary>
/// Pre-loads the OpenMP runtime that llama.cpp's CPU backend needs.
///
/// The wrapper's Backend.Init() calls ggml_backend_load_all_from_path(), and ggml loads its
/// backend libraries (ggml-cpu-*.dll) with a plain LoadLibraryW(fullPath) - see
/// ggml/src/ggml-backend-dl.cpp. That call does NOT search the directory of the DLL being
/// loaded when resolving its imports, and the package plugin folder is not on the process
/// search path either, so "libomp140.x86_64.dll" (a hard import of every ggml-cpu-*.dll in
/// the package) is never found. Release builds pass silent = true under NDEBUG, so the
/// failure is completely invisible and llama.cpp then aborts with
/// "make_cpu_buft_list: no CPU backend found" -> "unable to load model".
///
/// Loading libomp140.x86_64.dll by its full path first puts it into the process module
/// list, so the later import lookup by name succeeds and the CPU backend loads.
/// </summary>
internal static class LlamaNativeBootstrap
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private const string OpenMpRuntime = "libomp140.x86_64.dll";
    private const string LlamaDll = "llama.dll";

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryW")]
    private static extern IntPtr LoadLibrary(string fileName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetModuleHandleW")]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetModuleFileNameW")]
    private static extern uint GetModuleFileName(IntPtr module, StringBuilder fileName, int size);

    /// <summary>
    /// Runs before the first scene loads, and therefore before ChatCompletion.Start() calls
    /// LlamaCpp.Backend.Init().
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!TryResolvePluginDirectory(out string pluginDirectory))
        {
            Debug.LogWarning($"[LlamaCpp] Could not locate the native plugin folder, so {OpenMpRuntime} was not preloaded. " +
                             "The CPU backend may fail to load.");
            return;
        }

        string openMpPath = Path.Combine(pluginDirectory, OpenMpRuntime);
        if (!File.Exists(openMpPath))
        {
            Debug.LogWarning($"[LlamaCpp] {OpenMpRuntime} was not found in {pluginDirectory}. The CPU backend may fail to load.");
            return;
        }

        IntPtr handle = LoadLibrary(openMpPath);
        if (handle == IntPtr.Zero)
        {
            Debug.LogError($"[LlamaCpp] Failed to preload {openMpPath} (Win32 error {Marshal.GetLastWin32Error()}). " +
                           "The CPU backend may fail to load.");
            return;
        }

        Debug.Log($"[LlamaCpp] Preloaded {OpenMpRuntime} from {pluginDirectory}.");
    }

    private static bool TryResolvePluginDirectory(out string pluginDirectory)
    {
        // 1) llama.dll is already loaded (Editor after the plugins were loaded, and players).
        IntPtr module = GetModuleHandle(LlamaDll);
        if (module != IntPtr.Zero)
        {
            var buffer = new StringBuilder(1024);
            if (GetModuleFileName(module, buffer, buffer.Capacity) > 0)
            {
                string directory = Path.GetDirectoryName(buffer.ToString());
                if (!string.IsNullOrEmpty(directory) && File.Exists(Path.Combine(directory, OpenMpRuntime)))
                {
                    pluginDirectory = directory;
                    return true;
                }
            }
        }

        // 2) Windows player: native plugins live in <Game>_Data/Plugins[/x86_64].
        if (TryResolveFromCandidates(new[]
            {
                Path.Combine(Application.dataPath, "Plugins", "x86_64"),
                Path.Combine(Application.dataPath, "Plugins"),
            }, out pluginDirectory))
        {
            return true;
        }

        // 3) Editor: the package is a plain folder on disk, either resolved into the cache
        //    (<project>/Library/PackageCache/ai.lookbe.llamacpp@<hash>) or embedded
        //    (<project>/Packages/ai.lookbe.llamacpp). Note that "Packages/<name>" itself is a
        //    virtual path, so it cannot be probed with File.Exists.
        string projectRoot = null;
        try
        {
            DirectoryInfo projectDirectory = Directory.GetParent(Application.dataPath);
            if (projectDirectory != null)
            {
                projectRoot = projectDirectory.FullName;
            }
        }
        catch (Exception)
        {
            // ignored: fall through to the failure below
        }

        if (!string.IsNullOrEmpty(projectRoot))
        {
            string[] packageRoots =
            {
                Path.Combine(projectRoot, "Library", "PackageCache"),
                Path.Combine(projectRoot, "Packages"),
            };

            foreach (string packageRoot in packageRoots)
            {
                if (!Directory.Exists(packageRoot))
                {
                    continue;
                }

                foreach (string packageDirectory in Directory.GetDirectories(packageRoot, "ai.lookbe.llamacpp*"))
                {
                    if (TryResolveFromCandidates(new[]
                        {
                            Path.Combine(packageDirectory, "Plugins", "Windows", "x86-64"),
                            Path.Combine(packageDirectory, "Plugins", "Windows"),
                        }, out pluginDirectory))
                    {
                        return true;
                    }
                }
            }
        }

        pluginDirectory = null;
        return false;
    }

    private static bool TryResolveFromCandidates(string[] candidates, out string pluginDirectory)
    {
        foreach (string candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, OpenMpRuntime)))
            {
                pluginDirectory = candidate;
                return true;
            }
        }

        pluginDirectory = null;
        return false;
    }
#endif
}
