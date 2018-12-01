using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[System.Serializable]
public class PhysicScatterWindow : EditorWindow
{

    public bool dropMenuSelected;
    public bool explosionMenuSelected;
    public bool oldDropMenuSelected;
    public bool oldExplosionMenuSelected;
    bool[] physicModeCheck = new bool[2] { true, false };
    bool[] oldPhysicModeCheck = new bool[2] { true, false };
    public bool showEffectGraphics = false;

    private int mainMenuIndex = 0;

    private float dropFlux = 0.3f;
    private List<SpawnableObject> spawnableObjects = new List<SpawnableObject>();

    private Texture2D[] mainMenuIcons = new Texture2D[2];

    [SerializeField]
    private SpawnableObject objGlobalSettings = new SpawnableObject();


    enum PhysicsMode { AllRigidbodies, Scattered, Selected };
    enum PhysicsEffects { Explosion, BlackHole, SimpleForce };

    GameObject rootContainer;
    private Vector2 scrollView = Vector2.zero;
    private Vector3 lastSpawn;

    PhysicsEffects physicsEffects;
    PhysicsMode physicsMode = PhysicsMode.AllRigidbodies;

    private float brushSize = 1f;
    private float brushDotSize = 0.1f;

    string message = "Output"; 

#pragma warning disable 414
    //This object is indeed used, but for some reason the compiler thinks that it's not and returns a warning
    private SerializedObject so;
#pragma warning restore 414

    Stopwatch messageStopwatch = new Stopwatch(); 
    private int minSizeX = 410;


    bool showRootOption=true; 

    [MenuItem("Window/Physics Scatter")]
    static void ScatterMeneu()
    {
        PhysicScatterWindow window = (PhysicScatterWindow)EditorWindow.GetWindow(typeof(PhysicScatterWindow));
        window.Show();
    }

    void Update()
    {
        if (PhysicsUtilities.physicActive)
        {
            Physics.Simulate(Time.deltaTime);
        }
    }

    void OnEnable()
    {
        this.titleContent = new GUIContent("Physics Scatter");
        this.minSize = new Vector2(minSizeX, 100);
        InitMainIcons();
        so = new UnityEditor.SerializedObject(this);
        SceneView.onSceneGUIDelegate += onSceneGUI;
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= onSceneGUI;
    }

    private void OnGUI()
    {
        GUIutilities.WriteTitle("Global Options");

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginHorizontal();
        PhysicsUtilities.physicActive = EditorGUILayout.ToggleLeft(new GUIContent("Is Physics Active", "Toggle the physics engine - this will not affect the objects (RIGHT CTRL)' rigidbodies"), PhysicsUtilities.physicActive, GUILayout.Width(150));
        showEffectGraphics = EditorGUILayout.ToggleLeft(" Effects Graphics (ALT)", showEffectGraphics);
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        PhysicsUtilities.physicsScatter = EditorGUILayout.ToggleLeft(new GUIContent("Physics Scatter", "Scatter objects with rigidbodies and colliders, so that they will respond to physics (SHIFT+D)"), PhysicsUtilities.physicsScatter);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Dropping Flux", "Change the amount of objects spawned when mouse is dragged"), GUILayout.Width(100));
        dropFlux = EditorGUILayout.Slider(dropFlux, 0, 3);
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();


        //GUIutilities.WriteTitle("Root Options");
        GUIStyle labelStyle2 = new GUIStyle("foldout");
        labelStyle2.fontStyle = FontStyle.Bold;
        labelStyle2.fontSize = 11;
        labelStyle2.active.textColor = Color.black;

   

        showRootOption = GUIutilities.SimpleFoldOut("Root Options", showRootOption);
        if (showRootOption)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            if (rootContainer != null)
            {

                EditorGUILayout.LabelField("Name Root: " + rootContainer.name);
            }
            else
            {
                GUIStyle labelStyle = new GUIStyle("label");
                labelStyle.fontStyle = FontStyle.Bold;
                EditorGUILayout.LabelField("No root selected! Start dropping and a root will be created.", labelStyle, GUILayout.Height(20));
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Root"))
            {
                rootContainer = new GameObject("New Root Container");
            }
            if (GUILayout.Button(new GUIContent("Mark as Root", "Mark the selected gameObject in the hierarchy as a root")))
            {
                rootContainer = (Selection.activeObject as GameObject);

            }

            if (rootContainer == null)
            {
                GUI.enabled = false;
            }
            else
            {
                GUI.enabled = true;
            }
            if (GUILayout.Button(new GUIContent("Delete Root", "Delete current root including the child objects")))
            {
                if (EditorUtility.DisplayDialog("Delete Root", "Are you sure you want to delete the root container with all its object? This action cannot be undone", "Yes", "No"))
                    DestroyImmediate(rootContainer);

            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool buttonPressed = false;
            if (GUILayout.Button(new GUIContent("Delete Rbody in Root (SHIFT+A)", "Delete all Rigidbody in objects within " + rootContainer + " root. This will not delete colliders.")))
            {
                DeleteRigidbodyInRoot();
                buttonPressed = true;

            }
            if (GUILayout.Button(new GUIContent("Add Rbody in Root (SHIFT+S)", "Add Rigidbody to every objects within " + rootContainer + " root. This will not add colliders")))
            {
                AddRigidbodyInRoot();
                buttonPressed = true;

            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("Delete Colliders in Root", "Delete colliders in the objects within " + rootContainer + " root. This will not delete the rigidbodies.")))
            {
                DeleteColliderInRoot();
                buttonPressed = true;
            }
            if (GUILayout.Button(new GUIContent("Add Colliders in Root", "Add collider to every objects within " + rootContainer + " root. This will not add the rigibodies")))
            {
                AddColliderInRoot();
                buttonPressed = true;
            }
            if (buttonPressed)
            {
                messageStopwatch.Reset();
                messageStopwatch.Start();
            }

            EditorGUILayout.EndHorizontal();

            
                EditorGUILayout.LabelField(message, GUILayout.Height(18));
            

            if (messageStopwatch.ElapsedMilliseconds > 3000)
            {
                message = "Output";
                messageStopwatch.Reset();
            }

            EditorGUI.indentLevel--;
        }

        GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
        EditorGUILayout.Separator();

        int oldIndex = mainMenuIndex;

        if (mainMenuIndex == -1)
        {
            dropMenuSelected = false;
            explosionMenuSelected = false;
        }
        if (mainMenuIndex == 0)
        {
            dropMenuSelected = true;
            explosionMenuSelected = false;
        }
        if (mainMenuIndex == 1)
        {
            dropMenuSelected = false;
            explosionMenuSelected = true;
        }

        GUI.enabled = true;
        EditorGUILayout.BeginHorizontal();
        dropMenuSelected = GUILayout.Toggle(dropMenuSelected, mainMenuIcons[0], new GUIStyle("Button"));
        explosionMenuSelected = GUILayout.Toggle(explosionMenuSelected, mainMenuIcons[1], new GUIStyle("Button"));
        EditorGUILayout.EndHorizontal();

        if (dropMenuSelected != oldDropMenuSelected)
        {
            if (dropMenuSelected)
            {
                explosionMenuSelected = false;
                oldExplosionMenuSelected = false;
            }
        }
        if (explosionMenuSelected != oldExplosionMenuSelected)
        {
            if (explosionMenuSelected)
            {
                dropMenuSelected = false;
                oldDropMenuSelected = false;
            }
        }

        oldExplosionMenuSelected = explosionMenuSelected;
        oldDropMenuSelected = dropMenuSelected;

        if (dropMenuSelected)
        {
            mainMenuIndex = 0;
            showEffectGraphics = false;
        }
        else if (explosionMenuSelected)
        {
            mainMenuIndex = 1;
            showEffectGraphics = true;
        }
        else { mainMenuIndex = -1; }

        scrollView = EditorGUILayout.BeginScrollView(scrollView);


        switch (mainMenuIndex)
        {
            case -1:
                EditorGUILayout.LabelField("Created by Valerio Biscione");

                break;
            case 0:
                if (spawnableObjects.Count == 0)
                {
                    GUIStyle labelStyle = new GUIStyle("label");
                    labelStyle.fontStyle = FontStyle.Bold;
                    labelStyle.fontSize = 14;
                    labelStyle.normal.textColor = Color.red;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("To start, drop a gameObject from the hierarchy \ninto here!", labelStyle, GUILayout.Height(40));
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("Drop gameObjects here");
                }
                DropObjectsMenu();
                break;
            case 1:
                EditorGUILayout.HelpBox("Physics Effect. Hold down the SPACEBAR to activate the selected effect", MessageType.None);
                bool newSelection = false;
                if (oldIndex != mainMenuIndex) newSelection = true;
                DrawEffectMenu(newSelection);
                break;
        }


        CheckDragDrop();
        ShortCut();

        EditorGUILayout.EndScrollView();
    }


    private void DeleteRigidbodyInRoot()
    {
        bool doneSmt = false; 
        Transform rootTransform = rootContainer.transform;
        foreach (Transform child in rootTransform)
        {
            if (child.gameObject.GetComponent<Rigidbody>() != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject.GetComponent<Rigidbody>());
                doneSmt = true; 
            }
        }
        if (doneSmt)
        {
            message= "All Ridibodies deleted.";
            UndoTransformInRoot();

        }
        else
        {
            message = "None of the object contained a Rigidbody.";
        }
    }


    private void AddColliderInRoot()
    {
        bool newColl= false; 
        Transform rootTransform = rootContainer.transform;
        foreach (Transform child in rootTransform)
        {
            if (child.gameObject.GetComponent<Collider>() == null)
            {
                Undo.AddComponent<BoxCollider>(child.gameObject);
                newColl = true; 
            }
        }
        if (newColl)
        {
            message = "All Colliders added.";
            UndoTransformInRoot();
        }
        else
        {
            message = "All objects contained a Collider already.";
        }
    }

    private void DeleteColliderInRoot()
    {
        bool delete = true;
        if (CheckIfAtLeastOneRigidbodyInRoot())
        {
            delete = false;
            if (EditorUtility.DisplayDialog("Rigibodies in root", "At least one object in root has a Rigidbody. If you delete the Collider but not the Rigidbody, these objects will likely fall down forever. We suggest you delete the Rigidbodies to all first.", "Continue (delete only Colliders)", "Cancel"))
            {
                delete = true;
                ((SceneView)SceneView.sceneViews[0]).Focus();
            }
        }
        if (delete)
        {
            bool doneSmt = false; 
            Transform rootTransform = rootContainer.transform;
            foreach (Transform child in rootTransform)
            {
                if (child.gameObject.GetComponent<Collider>() != null)
                {
                    Undo.DestroyObjectImmediate(child.gameObject.GetComponent<Collider>());
                    doneSmt = true;
                }
            }
            if (doneSmt)
            {
                message="All Colliders deleted.";
                UndoTransformInRoot();
            }
            else
            {
                message = "None of the object contained a Collider.";
            }

        }
    }

    private void AddRigidbodyInRoot()
    {
        bool add = true;
        if (CheckIfAtLeastOneObjectIsWithoutAColliderInRoot())
        {
            add = false;
            if (EditorUtility.DisplayDialog("Objects without Colliders", "At least one object in root does not have a Collider. If you add a Rigidbody without a Collider, these objects will fall down forever. We suggest you add the Colliders to all first", "Continue (only add Rigidbodies)", "Cancel"))
            {
                ((SceneView)SceneView.sceneViews[0]).Focus();
                add = true;
            }
        }
        if (add)
        {
            bool doneSmt = false; 
            Transform rootTransform = rootContainer.transform;
            foreach (Transform child in rootTransform)
            {
                if (child.gameObject.GetComponent<Rigidbody>() == null)
                {
                    Undo.AddComponent < Rigidbody > (child.gameObject);
                    doneSmt = true;
                    UndoTransformInRoot();
                }
            }
            if (doneSmt)
            {
                message = "Rigidbodies added";
            }
            else
            {
                message = "All objects contained a Rigidbody already.";
            }
        }

    }

    private bool CheckIfAtLeastOneRigidbodyInRoot()
    {
        Transform rootTransform = rootContainer.transform;
        foreach (Transform child in rootTransform)
        {
            if (child.gameObject.GetComponent<Rigidbody>() != null)
            {
                return true;
            }
        }
        return false;

    }
    private bool CheckIfAtLeastOneObjectIsWithoutAColliderInRoot()
    {
        Transform rootTransform = rootContainer.transform;
        foreach (Transform child in rootTransform)
        {
            if (child.gameObject.GetComponent<Collider>() == null)
            {
                return true;
            }
        }
        return false;

    }


    void ShortCut()
    {
        //PUT THOSE WITH THE MODIFIERS ON TOP!! 
        if (Event.current.type == EventType.KeyDown && Event.current.modifiers == EventModifiers.Shift && Event.current.keyCode == KeyCode.S && rootContainer != null)
        {
            AddRigidbodyInRoot();
            return;
        }

        //PUT THOSE WITH THE MODIFIERS ON TOP!! 
        if (Event.current.type == EventType.KeyDown && Event.current.modifiers == EventModifiers.Shift && Event.current.keyCode == KeyCode.D)
        {
            PhysicsUtilities.physicsScatter = !PhysicsUtilities.physicsScatter;
            return;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.modifiers == EventModifiers.Shift &&
            Event.current.keyCode == KeyCode.A && rootContainer != null)
        {
            DeleteRigidbodyInRoot();
            return;
        }


        if (Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Q || Event.current.keyCode == KeyCode.W || Event.current.keyCode == KeyCode.R || Event.current.keyCode == KeyCode.E)
            && (mainMenuIndex == 0 || mainMenuIndex == 1))
        {
            mainMenuIndex = -1;
        }
        // Select Paint tool
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.A)
        {
            if (mainMenuIndex == 0)
            {
                mainMenuIndex = -1;
            }
            else
            {
                mainMenuIndex = 0;
            }
            return;
        }



        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S)
        {
            if (mainMenuIndex == 1)
            {
                mainMenuIndex = -1;
            }
            else
            {
                mainMenuIndex = 1;
            }
            return;
        }
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.RightControl)
        {
            PhysicsUtilities.physicActive = !PhysicsUtilities.physicActive;
            return;
        }

        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.LeftAlt || Event.current.keyCode == KeyCode.RightAlt))
        {
            showEffectGraphics = !showEffectGraphics;
            return;
        }
        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Space))
        {
            ((SceneView)SceneView.sceneViews[0]).Focus();
            return;
        }

    }


    private void DrawEffectMenu(bool newSelection)
    {
        if (newSelection) Tools.current = Tool.None;

        GUIutilities.WriteTitle("Physic Effects Mode");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space();

        physicModeCheck[0] = EditorGUILayout.ToggleLeft("All Rigidbodies", physicModeCheck[0], GUILayout.Width(150));
        if (rootContainer == null)
        {
            physicModeCheck[0] = true;
            GUI.enabled = false;
        }
        else
        {
            GUI.enabled = true;
        }
        physicModeCheck[1] = EditorGUILayout.ToggleLeft("Only in Root Container", physicModeCheck[1]);


        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        GUIutilities.WriteTitle("Physic Effect (SPACEBAR to activate)");
        for (int i = 0; i < oldPhysicModeCheck.Length; i++)
        {
            if (oldPhysicModeCheck[i] != physicModeCheck[i])
            {
                if (physicModeCheck[i] == true)
                {
                    for (int j = 0; j < oldPhysicModeCheck.Length; j++)
                    {
                        if (j == i) continue;
                        physicModeCheck[j] = false;
                    }
                    break;
                }
                else
                {
                    physicModeCheck[i] = true;
                }
            }
        }
        for (int i = 0; i < oldPhysicModeCheck.Length; i++)
        {
            oldPhysicModeCheck[i] = physicModeCheck[i];
            if (physicModeCheck[i])
            {
                physicsMode = (PhysicsMode)i;
            }
        }


        physicsEffects = (PhysicsEffects)GUILayout.SelectionGrid((int)physicsEffects, convertEnumToStringArray(), convertEnumToStringArray().Length);
        if (physicsEffects == PhysicsEffects.Explosion)
        {
            EditorGUILayout.Space();
            GUIutilities.SliderMinMax("Vertical Offset", ref ExplosionParams.minVerticalOffset, ref ExplosionParams.maxVerticalOffset, ref ExplosionParams.verticalOffset, 100);
            GUIutilities.SliderMinMax("Radius", ref ExplosionParams.minRadius, ref ExplosionParams.maxRadius, ref ExplosionParams.radius, 100);
            GUIutilities.SliderMinMax("Power", ref ExplosionParams.minPower, ref ExplosionParams.maxPower, ref ExplosionParams.power, 100);

        }
        if (physicsEffects == PhysicsEffects.SimpleForce)
        {
            EditorGUILayout.Space();
            GUIutilities.SliderMinMax("Vertical Offset", ref SimpleForceParams.minVerticalOffset, ref SimpleForceParams.maxVerticalOffset, ref SimpleForceParams.verticalOffset, 100);
            GUIutilities.SliderMinMax("Radius", ref SimpleForceParams.minRadius, ref SimpleForceParams.maxRadius, ref SimpleForceParams.radius, 100);
            GUIutilities.simpleSlider("powerX", ref SimpleForceParams.powerX, -100, 100, 100);
            GUIutilities.simpleSlider("powerY", ref SimpleForceParams.powerY, 0, 100, 100);
            GUIutilities.simpleSlider("powerZ", ref SimpleForceParams.powerZ, -100, 100, 100);
        }
        if (physicsEffects == PhysicsEffects.BlackHole)
        {
            EditorGUILayout.Space();
            GUIutilities.SliderMinMax("Vertical Offset", ref BlackHoleParams.minVerticalOffset, ref BlackHoleParams.maxVerticalOffset, ref BlackHoleParams.verticalOffset, 100);
            GUIutilities.SliderMinMax("Radius", ref BlackHoleParams.minRadius, ref BlackHoleParams.maxRadius, ref BlackHoleParams.radius, 100);
            GUIutilities.SliderMinMax("Power", ref BlackHoleParams.minPower, ref BlackHoleParams.maxPower, ref BlackHoleParams.power, 100);


            GUIutilities.SliderMinMax(new GUIContent("Modifier", "Modifies the internal equation for the black hole. A value between 1 and 2 appears to create a blob; a value from 0 to 1 creates an oscillating system. Not all values have been tested. Use at your own risk!!"), ref BlackHoleParams.minModifier, ref BlackHoleParams.maxModifier, ref BlackHoleParams.modifier, 100);

        }

    }

    public string[] convertEnumToStringArray()
    {
        return Enum.GetNames(typeof(PhysicsEffects));

    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    void onSceneGUI(SceneView sceneView)
    {

        ShortCut();

        Vector2 mousePos = Event.current.mousePosition;

        float pixelPerPoint = EditorGUIUtility.pixelsPerPoint;
        mousePos.y = Screen.height - (mousePos.y * pixelPerPoint) - (40 * pixelPerPoint);
        mousePos.x = pixelPerPoint * mousePos.x;


        Camera camera = UnityEditor.SceneView.lastActiveSceneView.camera;
        if (camera == null) return;
        Ray ray = camera.ScreenPointToRay(mousePos);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            if (mainMenuIndex == 0 || mainMenuIndex == 1)
            {

                // Disable noraml tools
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                Tools.current = Tool.None;

                // Click
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    SpawnObject(hit.point, hit.normal);
                }

                // Drag
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    if (Vector3.Distance(lastSpawn, hit.point) > 1 / dropFlux)
                    {
                        SpawnObject(hit.point, hit.normal);
                        lastSpawn = hit.point;
                    }
                }
            }

            if (Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Space)
            {

                PhysicsEffect(hit.point, hit.normal);
            }

            // DrawBrush
            if (mainMenuIndex == 0 || mainMenuIndex == 1)
            {
                GUIDrawBrush(hit.point, brushSize, hit.normal);
                DrawPhysicsEffect(hit.point);
            }
        }

        sceneView.Repaint();

    }


    void SpawnObject(Vector3 position, Vector3 normal)
    {
        if (rootContainer != null)
        {
            UndoTransformInRoot();
        }
        // Get selected gameobject
        List<SpawnableObject> objs = spawnableObjects.FindAll(
            delegate (SpawnableObject s)
            {
                return s.spawn == true;
            });
        if (objs.Count == 0)
        {
            return;
        }
        int rndObj = UnityEngine.Random.Range(0, objs.Count);

        SpawnableObject selectedObject = objs[rndObj];
        Vector3 positionUp = position;
        if (objGlobalSettings.spawn)
        {
            positionUp.y = positionUp.y + objGlobalSettings.offset;
        }
        else
        {
            positionUp.y = positionUp.y + selectedObject.offset;
        }
        GameObject newObj = Instantiate(selectedObject.prefab, positionUp, Quaternion.identity);

        // Apply random factors
        if (objGlobalSettings.spawn)
        {
            SpawnableObject.ApplyRandomFactors(objGlobalSettings, newObj.transform);
        }
        else
        {
            SpawnableObject.ApplyRandomFactors(selectedObject, newObj.transform);
        }

        // We add a box collider if no box collider is present. 
        if (newObj.GetComponent<Collider>() == null && PhysicsUtilities.physicsScatter)
        {
            newObj.AddComponent<BoxCollider>();
        }

        // If a collider is present and is a meshCollider, we have the problem that mesh colliders are not supported anymore. However, sometime when convex is set to true and error is thrown and convex fails. If we inflate the Mesh first, 
        // the convex mesh is created correctly.
        if (newObj.GetComponent<MeshCollider>() != null && PhysicsUtilities.physicsScatter && newObj.GetComponent<MeshCollider>().convex == false)
        {
            newObj.GetComponent<MeshCollider>().inflateMesh = true;
            newObj.GetComponent<MeshCollider>().convex = true;
        }

        // We add a rigidbody if not present 
        if (newObj.GetComponent<Rigidbody>() == null && PhysicsUtilities.physicsScatter)
        {
            newObj.AddComponent<Rigidbody>();
        }
        // If there is a rigidbody, and is kinematic, we set kinematic to false. 
        if (newObj.GetComponent<Rigidbody>() != null && PhysicsUtilities.physicsScatter && newObj.GetComponent<Rigidbody>().isKinematic == true)
        {
            newObj.GetComponent<Rigidbody>().isKinematic = false;
        }

        if (newObj.GetComponent<Rigidbody>() != null && PhysicsUtilities.physicsScatter == false)
        {
            DestroyImmediate(newObj.GetComponent<Rigidbody>());
        }


        newObj.transform.parent = returnOrCreateRootContainer().transform;
        Undo.RegisterCreatedObjectUndo(newObj, "Physics Scatter : new objectS");

        PhysicsUtilities.ActivatePhysics();

    }

    public GameObject returnOrCreateRootContainer()
    {
        if (rootContainer == null)
        {
            rootContainer = new GameObject("Root Container");
        }
        return rootContainer;
    }


    private void PhysicsEffect(Vector3 point, Vector3 normal)
    {
        if (physicsMode == PhysicsMode.Scattered)
        {
            PhysicsUtilities.CopyAllRigidbodies();
            PhysicsUtilities.DeleteRigidBodiesNotInParent(rootContainer);
        }
        switch (physicsEffects)
        {
            case PhysicsEffects.Explosion:
                PhysicsUtilities.AddExplosion(point, normal);
                break;
            case PhysicsEffects.BlackHole:
                PhysicsUtilities.AddBlackHole(point, normal);
                break;
            case PhysicsEffects.SimpleForce:
                PhysicsUtilities.AddSimpleForce(point, normal);
                break;
        }

        if (physicsMode == PhysicsMode.Scattered)
        {
            PhysicsUtilities.PasteRigidbodies();
            var rigidbodyContainer = GameObject.Find("Rigidbody Container");
            GameObject.DestroyImmediate(rigidbodyContainer);
        }
        PhysicsUtilities.ActivatePhysics();
    }



    void AddPrefabOptions(SpawnableObject spawnablePrefab, bool isGlobal)
    {
        EditorGUI.indentLevel++;

        bool isDelete = false;

        EditorGUILayout.BeginHorizontal();
        // Enable
        spawnablePrefab.spawn = GUILayout.Toggle(spawnablePrefab.spawn, spawnablePrefab.prefab != null ? spawnablePrefab.prefab.name : "Global settings", new GUIStyle("Button"), GUILayout.Width(100));
        GUI.enabled = !spawnablePrefab.optionDelete;

        // Disable other button
        GUI.enabled = true;

        // options
        if (!spawnablePrefab.optionDelete)
        {
            spawnablePrefab.isRotationX = GUILayout.Toggle(spawnablePrefab.isRotationX, "X", new GUIStyle("Button"));
            spawnablePrefab.isRotationY = GUILayout.Toggle(spawnablePrefab.isRotationY, "Y", new GUIStyle("Button"));
            spawnablePrefab.isRotationZ = GUILayout.Toggle(spawnablePrefab.isRotationZ, "Z", new GUIStyle("Button"));
            spawnablePrefab.isScale = GUILayout.Toggle(spawnablePrefab.isScale, "Scale", new GUIStyle("Button"));
            spawnablePrefab.showOption = GUILayout.Toggle(spawnablePrefab.showOption, "See Settings", new GUIStyle("Button"));
            if (isGlobal)
            {

                if (GUILayout.Toggle(spawnablePrefab.optionDelete, "Delete", new GUIStyle("Button"), GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Delete All Spawning Object", "This will delete all the object set for spawning. Are you sure to continue?", "Yes", "No"))
                    {
                        spawnableObjects = new List<SpawnableObject>();

                    }
                }

            }


        }

        // Delete
        if (!isGlobal)
        {
            if (!spawnablePrefab.optionDelete)
            {
                spawnablePrefab.optionDelete = GUILayout.Toggle(spawnablePrefab.optionDelete, "Delete", new GUIStyle("Button"), GUILayout.Width(60));
            }
            else
            {
                if (GUIutilities.Button("Back", Color.white, 50, 18))
                {
                    spawnablePrefab.optionDelete = false;
                }
                if (GUIutilities.Button("Delete", Color.white, 50, 18))
                {
                    spawnableObjects.Remove(spawnablePrefab);
                    isDelete = true;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!isDelete && spawnablePrefab.showOption)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();


            EditorGUILayout.LabelField("Vertical Offset", GUILayout.Width(100));
            spawnablePrefab.minOffset = EditorGUILayout.FloatField("", spawnablePrefab.minOffset, GUILayout.Width(50));
            spawnablePrefab.offset = EditorGUILayout.Slider(spawnablePrefab.offset, spawnablePrefab.minOffset, spawnablePrefab.maxOffset);
            spawnablePrefab.maxOffset = EditorGUILayout.FloatField("", spawnablePrefab.maxOffset, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            if (spawnablePrefab.isScale)
            {
                GUIutilities.sliderMinMaxRange("Scale", ref spawnablePrefab.minScale, ref spawnablePrefab.maxScale, 0, 10);
            }

            if (spawnablePrefab.isRotationX)
            {
                GUIutilities.sliderMinMaxRange("X", ref spawnablePrefab.xMinRot, ref spawnablePrefab.xMaxRot, -180, 180);
            }
            if (spawnablePrefab.isRotationY)
            {
                GUIutilities.sliderMinMaxRange("Y", ref spawnablePrefab.yMinRot, ref spawnablePrefab.yMaxRot, -180, 180);
            }
            if (spawnablePrefab.isRotationZ)
            {
                GUIutilities.sliderMinMaxRange("Z", ref spawnablePrefab.zMinRot, ref spawnablePrefab.zMaxRot, -180, 180);
            }

            EditorGUILayout.Space();
        }
        if (isGlobal)
        {
            EditorGUILayout.Space();
        }
        EditorGUI.indentLevel--;
    }


    void CheckDragDrop()
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {

                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        DoDropping(draggedObject);
                        mainMenuIndex = 0;
                    }
                }
                break;
        }
    }

    void UndoTransformInRoot()
    {
        Transform rootTransform = rootContainer.transform;
        foreach (Transform child in rootTransform)
        {
            Undo.RecordObject(child.gameObject.transform, "Physics Scatter : Undo Transform In Root");
        }
    }
    
    void DoDropping(UnityEngine.Object droppedObject)
    {

        if (droppedObject.GetType() == typeof(GameObject) || droppedObject.GetType() == typeof(Transform))
        {
            GameObject obj = null;
            if (droppedObject.GetType() == typeof(GameObject))
            {
                obj = (GameObject)droppedObject;
            }
            else
            {
                obj = ((Transform)droppedObject).gameObject;
            }
            //Check if prefab was already put here 
            int result = spawnableObjects.FindIndex(
                delegate (SpawnableObject s)
                {
                    return s.prefab == obj || (s.prefab.name == obj.name && s.prefab != obj);
                }
            );


            if (result == -1)
            {
                SpawnableObject so = new SpawnableObject();
                so.prefab = obj;
                so.spawn = true;
                so.isPrefab = PrefabUtility.GetPrefabType(obj) == PrefabType.Prefab ? true : false;

                spawnableObjects.Add(so);
            }
        }
    }


    void DropObjectsMenu()
    {
        // Title 
        GUIutilities.WriteTitle("Add Prefabs");

        EditorGUILayout.BeginVertical();

        AddPrefabOptions(objGlobalSettings, true);

        for (int i = 0; i < spawnableObjects.Count; i++)
        {
            if (spawnableObjects[i].prefab != null)
            {
                AddPrefabOptions(spawnableObjects[i], false);
            }
            else
            {
                spawnableObjects.Remove(spawnableObjects[i]);
            }

        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    void DrawPhysicsEffect(Vector3 position)
    {
        if (showEffectGraphics)
        {
            if (physicsEffects == PhysicsEffects.Explosion)
            {
                Vector3 positionUp = position;
                positionUp.y = position.y + ExplosionParams.verticalOffset;
                var transp = (ExplosionParams.power / ExplosionParams.maxPower) * 0.6f;

                Handles.color = new Vector4(1, 0, 0, transp);

                Handles.SphereHandleCap(0, positionUp, Quaternion.identity, ExplosionParams.radius * 2, EventType.Repaint);
                Handles.color = Color.red;
                Handles.DrawDottedLine(position, positionUp, 2f);
                Handles.SphereHandleCap(0, positionUp, Quaternion.identity, 0.3f, EventType.Repaint);

            }

            if (physicsEffects == PhysicsEffects.SimpleForce)
            {
                Vector3 positionUp = position;
                positionUp.y = position.y + SimpleForceParams.verticalOffset;

                Handles.color = new Vector4(0, 1, 0, 0.2f);

                Handles.SphereHandleCap(0, positionUp, Quaternion.identity, SimpleForceParams.radius * 2, EventType.Repaint);
                Handles.color = Color.green;
                Handles.DrawDottedLine(position, positionUp, 2f);

                Handles.SphereHandleCap(0, positionUp, Quaternion.identity, 0.3f, EventType.Repaint);
                var normVect = Vector3.Normalize(new Vector3(SimpleForceParams.powerX, SimpleForceParams.powerY, SimpleForceParams.powerZ));
                Handles.DrawLine(positionUp, positionUp + normVect * SimpleForceParams.radius);
                Handles.SphereHandleCap(0, positionUp + normVect * SimpleForceParams.radius, Quaternion.identity, 0.3f, EventType.Repaint);


            }

            if (physicsEffects == PhysicsEffects.BlackHole)
            {
                Vector3 positionUp = position;
                positionUp.y = position.y + BlackHoleParams.verticalOffset;
                var transp = (BlackHoleParams.power / BlackHoleParams.maxPower) * 0.6f;
                Handles.color = new Vector4(0, 0, 0, transp);

                Handles.SphereHandleCap(0, positionUp, Quaternion.identity, BlackHoleParams.radius * 2, EventType.Repaint);
                Handles.color = Color.black;
                Handles.DrawDottedLine(position, positionUp, 2f);
                Handles.SphereHandleCap(0, positionUp, Quaternion.identity, 0.3f, EventType.Repaint);

            }
        }
    }
    void GUIDrawBrush(Vector3 position, float brushSize, Vector3 normal)
    {
        Handles.color = Color.blue;
        Handles.DrawLine(new Vector3(position.x - brushSize, position.y, position.z - brushSize),
         new Vector3(position.x + brushSize, position.y, position.z + brushSize));
        Handles.DrawLine(new Vector3(position.x + brushSize, position.y, position.z - brushSize),
         new Vector3(position.x - brushSize, position.y, position.z + brushSize));
        Handles.color = Color.blue;
        Handles.DrawSolidDisc(position, normal, brushDotSize);

        if (objGlobalSettings.spawn)
        {
            Handles.color = Color.blue;
            var positionUp = position;
            positionUp.y = positionUp.y + objGlobalSettings.offset;
            Handles.DrawDottedLine(position, positionUp, 2f);
            Handles.SphereHandleCap(0, positionUp, Quaternion.identity, 0.3f, EventType.Repaint);
            //Handles.DrawLines
        }
    }

    void InitMainIcons()
    {
        if (mainMenuIcons[0] == null)
        {
            mainMenuIcons[0] = (Texture2D)Resources.Load("paint");
        }
        if (mainMenuIcons[1] == null)
        {
            mainMenuIcons[1] = (Texture2D)Resources.Load("explosion");
        }
    }
}
