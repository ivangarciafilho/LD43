#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Linq;

public class NGSS_LibrariesSetup : ScriptableWizard
{
    string cgIncludesPath = null;
    string installLog = null;
    bool installLocalLibrary = false;
    bool localLibrary = false;
    bool installDirectionalLibrary = false;
    bool directionalLibrary = false;
    bool displayRestart = false;
    bool isInit = false;

    [MenuItem("Tools/Psychose Interactive/NGSS Libraries Setup (Default Renderer)")]
    public static void WizardSetups()
    {
        DisplayWizard("NGSS Libraries Setup", typeof(NGSS_LibrariesSetup), "LibrariesSetup");
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    void OnGUI()
    {
        minSize = new Vector2(350, 295);
        maxSize = new Vector2(350, 295);

        if (!isInit)
        {
            try
            {
                var entryAssembly = new StackTrace().GetFrames().Last().GetMethod().Module.Assembly;
                var managedDir = Path.GetDirectoryName(entryAssembly.Location);
                //UnityEngine.Debug.Log(Directory.GetParent(managedDir).Parent.FullName);
                cgIncludesPath = managedDir + "/../CGIncludes/";
                //cgIncludesPath = Path.Combine(Directory.GetParent(managedDir).Parent.FullName, "CGIncludes/");

                if (!Directory.Exists(cgIncludesPath))
                {
                    cgIncludesPath = managedDir + "/../../CGIncludes/";
                    //cgIncludesPath = Path.Combine(Directory.GetParent(managedDir).Parent.Parent.FullName, "CGIncludes/");
                }
                if (!Directory.Exists(cgIncludesPath))
                {
                    UnityEngine.Debug.LogError("Can't find directory: " + cgIncludesPath);
                    return;
                }

                UnityEngine.Debug.Log("Current CGIncludes path: " + cgIncludesPath);
                UnityEngine.Debug.Log("Current Editor Assets Path: " + Application.dataPath);

                //override Directory and file access attributes (could posibly fail on Mac and Linux)
                //RemoveDirectorySecurity(cgIncludesPath, System.Security.Principal.WindowsIdentity.GetCurrent().Name, FileSystemRights.FullControl, AccessControlType.Allow);
                //File.SetAttributes(cgIncludesPath + "UnityShadowLibrary.cginc", FileAttributes.Normal);
                //File.SetAttributes(cgIncludesPath + "AutoLight.cginc", FileAttributes.Normal);

                //initialize button toggle states based on bak file exist
                installLocalLibrary = File.Exists(cgIncludesPath + "UnityShadowLibrary.cginc.bak");
                localLibrary = installLocalLibrary;

                installDirectionalLibrary = File.Exists(cgIncludesPath + "AutoLight.cginc.bak") && File.Exists(cgIncludesPath + "UnityDeferredLibrary.cginc.bak");
                directionalLibrary = installDirectionalLibrary;

                isInit = true;
            }
            catch (System.Exception ex)
            {
                GUI.Label(new Rect(10, 15, 330, 75), ex.Message);
                UnityEngine.Debug.LogWarning(ex.Message);
                return;
            }
        }

        //GUIStyle mystyle = new GUIStyle("style string name");
        if (displayRestart)
            GUI.Label(new Rect(20, 15, 310, 85), installLog, "TextArea");
        else
            GUI.Label(new Rect(20, 15, 310, 85), "This Wizard assist you with the installation and removal of NGSS internal libraries. Unity must be run with admin priviledges when using this tool (Run as administrator).\nNote: You need to install NGSS internal libraries using this tool every time you install or update Unity.", "TextArea");

        GUI.BeginGroup(new Rect(10, 80, 330, 260), "");
        if (isInit)
        {
            installLocalLibrary = GUI.Toggle(new Rect(10, 30, 310, 50), installLocalLibrary, !installLocalLibrary ? "Install NGSS Spot/Point libraries" : "Uninstall NGSS Spot/Point libraries", "Button");
            installDirectionalLibrary = GUI.Toggle(new Rect(10, 90, 310, 50), installDirectionalLibrary, !installDirectionalLibrary ? "Install Directional libraries" : "Uninstall Directional libraries", "Button");
            if (GUI.Button(new Rect(10, 150, 310, 50), "Delete ShaderCache folder only", "Button"))
            {
                if (Directory.Exists(Application.dataPath + "/../Library/ShaderCache"))
                {
                    Directory.Delete(Application.dataPath + "/../Library/ShaderCache", true);
                    installLog = "ShaderCache folder successfully deleted. Please restart the Editor to rebuild project ShaderCache.";
                    UnityEngine.Debug.Log(installLog);
                }
                else
                {
                    installLog = "ShaderCache folder not found. Please restart the Editor to rebuild project ShaderCache.";
                    UnityEngine.Debug.Log(installLog);
                }
            }

            if (localLibrary != installLocalLibrary)
            {
                try
                {
                    displayRestart = true;
                    localLibrary = installLocalLibrary;
                    if (installLocalLibrary)//install it
                    {
                        if (File.Exists(Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityShadowLibrary.cginc"))
                        {
                            //File.Replace(Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityShadowLibrary.cginc", cgIncludesPath + "/UnityShadowLibrary.cginc", cgIncludesPath + "/UnityShadowLibrary.cginc.bak");
                            File.Move(cgIncludesPath + "/UnityShadowLibrary.cginc", cgIncludesPath + "/UnityShadowLibrary.cginc.bak");//rename
                            File.Copy(Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityShadowLibrary.cginc", cgIncludesPath + "/UnityShadowLibrary.cginc", true);//copy

                            installLog = "NGSS Local libraries successfully installed. Unity Local libraries have been backed up. Make sure your scene lights have it's corresponding NGSS components.\nPlease restart the Editor to apply changes.";
                            UnityEngine.Debug.Log(installLog);
                        }
                        else
                        {
                            //GUI.Label(new Rect(10, 15, 330, 75), Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityShadowLibrary.cginc does not exist make sure the file exist!");
                            installLog = Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityShadowLibrary.cginc does not exist make sure the file exist!";
                            UnityEngine.Debug.LogWarning(installLog);
                        }
                    }
                    else//revert it
                    {
                        //UnityShadowLibrary
                        string shadowLibraryPath = Path.Combine(cgIncludesPath, "UnityShadowLibrary.cginc");
                        string shadowLibraryBakPath = Path.Combine(cgIncludesPath, "UnityShadowLibrary.cginc.bak");
                        if (File.Exists(shadowLibraryPath))
                            File.Delete(shadowLibraryPath);
                        if (File.Exists(shadowLibraryBakPath))
                            File.Move(shadowLibraryBakPath, shadowLibraryPath);

                        installLog = "Unity Local libraries successfully restored. NGSS Local libraries have been deleted.\nPlease restart the Editor to apply changes.";
                        UnityEngine.Debug.Log(installLog);
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.Log(ex.Message);
                    installLog = ex.Message;
                    installLocalLibrary = !installLocalLibrary;
                    localLibrary = installLocalLibrary;
                }
                finally
                {
                    if (Directory.Exists(Application.dataPath + "/../Library/ShaderCache"))
                        Directory.Delete(Application.dataPath + "/../Library/ShaderCache", true);
                }
            }

            if (directionalLibrary != installDirectionalLibrary)
            {
                try
                {
                    displayRestart = true;
                    directionalLibrary = installDirectionalLibrary;
                    if (installDirectionalLibrary)//install it
                    {
                        if (File.Exists(Application.dataPath + "/Psychose Interactive/NGSS/Libraries/AutoLight.cginc"))
                        {
                            //File.Replace(Application.dataPath + "/Psychose Interactive/NGSS/Libraries/AutoLight.cginc", cgIncludesPath + "/AutoLight.cginc", cgIncludesPath + "/AutoLight.cginc.bak");
                            File.Move(cgIncludesPath + "/AutoLight.cginc", cgIncludesPath + "/AutoLight.cginc.bak");//rename
                            File.Copy(Application.dataPath + "/Psychose Interactive/NGSS/Libraries/AutoLight.cginc", cgIncludesPath + "/AutoLight.cginc", true);//copy
                        }
                        else
                        {
                            //GUI.Label(new Rect(10, 15, 330, 75), Application.dataPath + "/Psychose Interactive/NGSS/Libraries/AutoLight.cginc does not exist make sure the file exist!");
                            installLog = Application.dataPath + "/Psychose Interactive/NGSS/Libraries/AutoLight.cginc does not exist make sure the file exist!";
                            UnityEngine.Debug.LogWarning(installLog);
                        }

                        if (File.Exists(Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityDeferredLibrary.cginc"))
                        {
                            //File.Replace(Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityDeferredLibrary.cginc", cgIncludesPath + "/UnityDeferredLibrary.cginc", cgIncludesPath + "/UnityDeferredLibrary.cginc.bak");
                            File.Move(cgIncludesPath + "/UnityDeferredLibrary.cginc", cgIncludesPath + "/UnityDeferredLibrary.cginc.bak");//rename
                            File.Copy(Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityDeferredLibrary.cginc", cgIncludesPath + "/UnityDeferredLibrary.cginc", true);//copy
                        }
                        else
                        {
                            //GUI.Label(new Rect(10, 15, 330, 75), Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityDeferredLibrary.cginc does not exist make sure the file exist!");
                            installLog = Application.dataPath + "/Psychose Interactive/NGSS/Libraries/UnityDeferredLibrary.cginc does not exist make sure the file exist!";
                            UnityEngine.Debug.LogWarning(installLog);
                        }

                        installLog = "NGSS Directional libraries files successfully installed. Unity Directional libraries have been backed up. Make sure your scene lights have it's corresponding NGSS components.\nPlease restart the Editor to apply changes.";
                        UnityEngine.Debug.Log(installLog);
                    }
                    else//revert it
                    {
                        //AutoLight
                        string autoLightPath = Path.Combine(cgIncludesPath, "AutoLight.cginc");
                        string autoLighBaktPath = Path.Combine(cgIncludesPath, "AutoLight.cginc.bak");
                        if (File.Exists(autoLightPath))
                            File.Delete(autoLightPath);
                        if (File.Exists(autoLighBaktPath))
                            File.Move(autoLighBaktPath, autoLightPath);

                        //UnityDeferredLibrary
                        string deferredLightPath = Path.Combine(cgIncludesPath, "UnityDeferredLibrary.cginc");
                        string deferredLightBakPath = Path.Combine(cgIncludesPath, "UnityDeferredLibrary.cginc.bak");
                        if (File.Exists(deferredLightPath))
                            File.Delete(deferredLightPath);
                        if (File.Exists(deferredLightBakPath))
                            File.Move(deferredLightBakPath, deferredLightPath);

                        installLog = "Unity Directional libraries successfully restored. NGSS Directional libraries have been deleted.\nPlease restart the Editor to apply changes.";
                        UnityEngine.Debug.Log(installLog);
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.Log(ex.Message);
                    installLog = ex.Message;
                    installDirectionalLibrary = !installDirectionalLibrary;
                    directionalLibrary = installDirectionalLibrary;
                }
                finally
                {
                    if (Directory.Exists(Application.dataPath + "/../Library/ShaderCache"))
                        Directory.Delete(Application.dataPath + "/../Library/ShaderCache", true);
                }
            }
        }
        else
        {
            GUI.Label(new Rect(10, 15, 330, 75), "NGSS was unable to find the Unity CGIncludes folder.");
            //UnityEngine.Debug.LogWarning("NGSS was unable to find the Unity CGIncludes folder.");
        }
        GUI.EndGroup();
    }
}
#endif
