﻿using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// 生成服务器navmesh脚本
/// </summary>
public class NavMeshWindow : EditorWindow {
    private int mapID;
    private GameObject map;
    private GameObject walkLayer;
    private MapProperty property; //地图存储属性

    [MenuItem("地图/生成寻路数据")]
    static void ShowSkillDebugWindow(){
        Rect wr = new Rect(0, 0, 500, 400);
        NavMeshWindow window =
            (NavMeshWindow) EditorWindow.GetWindowWithRect(typeof(NavMeshWindow), wr, true, "寻路数据生成");
        window.Show();
    }

    public void Show(){
        base.Show();
        init();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private void init(){
        ClearTmp();
        try {
            map = GameObject.Find("Map");
            if (map == null) {
                Debug.LogError("地图名和地图中场景节点名不一致或已隐藏地图节点，请打开");
                Close();
                return;
            }

            Debug.LogWarning("当前地图：" + EditorSceneManager.GetActiveScene().name);
            property = map.GetComponent<MapProperty>();
            if (property == null) {
                property = map.AddComponent<MapProperty>();
            }

            try {
                mapID = int.Parse(Path.GetFileNameWithoutExtension(EditorSceneManager.GetActiveScene().name)
                    .Replace("map", ""));
            }
            catch (System.Exception) {
                Debug.LogError("地图id转换错误，请确保场景节点名字为：map数字 格式");
                Close();
            }

            walkLayer = GameObject.Find("WalkLayer");
            if (walkLayer == null) {
                Debug.LogError("行走层找不到，请确保名字为WalkLayer");
                Close();
                return;
            }
        }
        catch (System.Exception) {
            Close();
        }
    }

    private void OnDestroy(){
        mapID = 0;
        map = null;
    }

    void OnGUI(){
        EditorGUILayout.LabelField("注意事项1：请确保地图节点为map数字形式");
        EditorGUILayout.LabelField("注意事项2：行走层命名为WalkLayer");
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("当前地图id为：" + mapID);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("请输入地图参数");

        property.endX = EditorGUILayout.FloatField("地图截止X坐标", property.endX);
        property.endZ = EditorGUILayout.FloatField("地图截止Z坐标", property.endZ);
        property.startX = EditorGUILayout.FloatField("地图起始X坐标", property.startX);
        property.startZ = EditorGUILayout.FloatField("地图起始Z坐标", property.startZ);
        EditorGUILayout.FloatField("地图宽度", property.endX - property.startX);
        EditorGUILayout.FloatField("地图高度", property.endZ - property.startZ);
        if (GUILayout.Button("测试地图大小")) {
            CreateMapTestMesh();
        }

        EditorGUILayout.Separator();
        if (GUILayout.Button("生成寻路数据")) {
            if (EditorSceneManager.GetActiveScene().name.Contains(mapID.ToString())) {
                CreateNavMeshData();
            }
        }

        EditorGUILayout.Separator();

        if (GUILayout.Button("重载配置")) {
            init();
        }
    }

    /// <summary>
    /// 创建测试地图大小
    /// </summary>
    void CreateMapTestMesh(){
        map.SetActive(true);
        GameObject UnWalkAble = CreateOb("MapTest", 0, new Mesh());
        Mesh UnWalkMesh = UnWalkAble.GetComponent<MeshFilter>().sharedMesh;
        UnWalkMesh.vertices = new Vector3[] {
            new Vector3(property.startX, 0, property.startZ),
            new Vector3(property.startX, 0, property.endZ + property.startZ),
            new Vector3(property.endX + property.startX, 0, property.endZ + property.startZ),
            new Vector3(property.endX + property.startX, 0, property.startZ)
        };
        UnWalkMesh.triangles = new int[] {0, 1, 2, 0, 2, 3};
    }

    /// <summary>
    /// 渲染行走层
    /// </summary>
    /// <param name="agentRadius"></param>
    void BuildFloorNavMesh(float agentRadius){
        map.SetActive(false);
        SetAgentRadius(agentRadius);
        //walkLayer.GetComponent<Renderer>().enabled = true;
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        //walkLayer.GetComponent<Renderer>().enabled = false;
        map.SetActive(true);
    }


    void BuildAllNavMesh(float agentRadius){
        map.SetActive(false);
        ClearTmp();
        SetAgentRadius(agentRadius);
        //walkLayer.GetComponent<Renderer>().enabled = true;
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        //walkLayer.GetComponent<Renderer>().enabled = false;
        map.SetActive(true);
    }

    /// <summary>
    /// 清除临时属性
    /// </summary>
    void ClearTmp(){
        GameObject MapNavMeshResult = GameObject.Find("MapNavMeshResult");
        if (MapNavMeshResult) {
            Object.DestroyImmediate(MapNavMeshResult);
        }

        GameObject MapTest = GameObject.Find("MapTest");
        if (MapTest) {
            Object.DestroyImmediate(MapTest);
        }

        GameObject NavMesh_WalkAble = GameObject.Find("NavMesh_WalkAble");
        if (NavMesh_WalkAble) {
            Object.DestroyImmediate(NavMesh_WalkAble);
        }

        GameObject NavMesh_UnWalkAble = GameObject.Find("NavMesh_UnWalkAble");
        if (NavMesh_UnWalkAble) {
            Object.DestroyImmediate(NavMesh_UnWalkAble);
        }
    }

    private static DateTime startTime;

    static void LogElapseTime(string tag = ""){
        var ms = (DateTime.Now - startTime).TotalMilliseconds;
        Debug.LogWarning($"{tag} use time:{ms} ms");
        startTime = DateTime.Now;
    }

    
    /// <summary>
    /// 创建navmesh数据
    /// </summary>
    void CreateNavMeshData(){
        ClearTmp();
        startTime = DateTime.Now;
        map.SetActive(false);
        if (walkLayer.GetComponent<Renderer>() != null) {
            walkLayer.GetComponent<Renderer>().enabled = false;
        }

        UnityEngine.AI.NavMeshTriangulation triangulatedNavMesh = UnityEngine.AI.NavMesh.CalculateTriangulation();
        LogElapseTime("CalculateTriangulation");
        GameObject WalkAble = CreateOb("NavMesh_WalkAble", 1, new Mesh());
        Vector3[] pathVertices = triangulatedNavMesh.vertices;
        int[] triangles = triangulatedNavMesh.indices;
        Mesh WalkMesh = WalkAble.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertexes = new Vector3[pathVertices.Length];
        for (int i = 0; i < pathVertices.Length; i++) {
            float x = pathVertices[i].x;
            float z = pathVertices[i].z;
            vertexes[i] = new Vector3(x, 0, z);
        }

        WalkMesh.vertices = vertexes;
        WalkMesh.triangles = triangles;

        GameObject UnWalkAble = CreateOb("NavMesh_UnWalkAble", 0, new Mesh());
        Mesh UnWalkMesh = UnWalkAble.GetComponent<MeshFilter>().sharedMesh;
        UnWalkMesh.vertices = new Vector3[] {
            new Vector3(property.startX, 0, property.startZ),
            new Vector3(property.startX, 0, property.endZ),
            new Vector3(property.endX, 0, property.endZ),
            new Vector3(property.endX, 0, property.startZ)
        };
        UnWalkMesh.triangles = new int[] {0, 1, 2, 0, 2, 3};
        SetAgentRadius(property.agentRadius);
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        LogElapseTime("BuildNavMesh");
        Object.DestroyImmediate(WalkAble);
        Object.DestroyImmediate(UnWalkAble);
        string path = Path.Combine(Application.dataPath, "Resources/Maps/");
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }

        string filename = path + mapID + ".navmesh.json";
        triangulatedNavMesh = UnityEngine.AI.NavMesh.CalculateTriangulation();
        string data = MeshToString(triangulatedNavMesh, 1);
        LogElapseTime("Re CalculateTriangulation");
        if (data.Length < 128) {
            alert("阻挡未打入！");
            return;
        }

        StringBuilder sb = new StringBuilder("{");
        sb.Append("\"mapID\":").Append(mapID);
        sb.Append(",\"startX\":").Append(property.startX).Append(",\"startZ\":").Append(property.startZ);
        sb.Append(",\"endX\":").Append(property.endX).Append(",\"endZ\":").Append(property.endZ);
        sb.Append(",").Append(data).Append("}");
        LogElapseTime("Build str");
        MeshToFile(filename, sb.ToString());
        LogElapseTime("MeshToFile");
        map.SetActive(true);
        alert("成功！");

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        var MapNavMeshResult = new Mesh();
        MapNavMeshResult.vertices = triangulatedNavMesh.vertices;
        MapNavMeshResult.triangles = triangulatedNavMesh.indices;
        MapNavMeshResult.RecalculateBounds();
        GameObject ob = GameObject.Find("MapNavMeshResult");
        if (ob == null) {
            ob = new GameObject("MapNavMeshResult");
            ob.AddComponent<MeshFilter>().mesh = MapNavMeshResult; //网格
            ob.AddComponent<MeshRenderer>(); //网格渲染器  
        }
        //Close();
    }

    private void alert(string content){
        this.ShowNotification(new GUIContent(content));
    }

    /// <summary>
    /// 创建对象
    /// </summary>
    /// <param name="name"></param>
    /// <param name="WalkLayer"></param>
    /// <returns></returns>
    private GameObject CreateOb(string name, int WalkLayer, Mesh walkMesh){
        GameObject ob = GameObject.Find(name);
        walkMesh.name = name;
        if (ob == null) {
            ob = new GameObject(name);
            ob.AddComponent<MeshFilter>(); //网格
            ob.AddComponent<MeshRenderer>(); //网格渲染器  
        }

        ob.GetComponent<MeshFilter>().sharedMesh = walkMesh;
        GameObjectUtility.SetStaticEditorFlags(ob, StaticEditorFlags.NavigationStatic);
        GameObjectUtility.SetNavMeshArea(ob, WalkLayer);
        return ob;
    }

    /// <summary>
    /// 设置agent属性
    /// </summary>
    /// <param name="agentRadius"></param>
    private void SetAgentRadius(float agentRadius){
        SerializedObject settingsObject = new SerializedObject(UnityEditor.AI.NavMeshBuilder.navMeshSettingsObject);
        SerializedProperty agentRadiusSettings = settingsObject.FindProperty("m_BuildSettings.agentRadius");

        agentRadiusSettings.floatValue = agentRadius;

        settingsObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="type">0 阻挡 ；1行走；2安全</param>
    /// <returns></returns>
    static string MeshToString(UnityEngine.AI.NavMeshTriangulation mesh, int type){
        if (mesh.indices.Length < 1) {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.Append(type == 0 ? "\"blockTriangles\":[" : (type == 1 ? "\"pathTriangles\":[" : "\"safeTriangles\":["));
        for (int i = 0; i < mesh.indices.Length; i++) {
            sb.Append(mesh.indices[i]).Append(",");
        }

        sb.Length--;
        sb.Append("],");

        sb.Append(type == 0 ? "\"blockVertices\":[" : (type == 1 ? "\"pathVertices\":[" : "\"safeVertices\":["));
        for (int i = 0; i < mesh.vertices.Length; i++) {
            Vector3 v = mesh.vertices[i];
            if (type > 0 && v.y < 1) {
                Debug.LogWarning("寻路mesh坐标小于1" + v.y);
            }

            sb.Append("{\"x\":").Append(v.x).Append(",\"y\":").Append(type == 0 ? 0 : v.y).Append(",\"z\":").Append(v.z)
                .Append("},");
        }

        sb.Length--;
        sb.Append("]");
        return sb.ToString();
    }

    static void MeshToFile(string filename, string meshData){
        using (StreamWriter sw = new StreamWriter(filename)) {
            sw.Write(meshData);
        }
    }
}