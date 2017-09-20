using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode()]
public class CurveImplementation : MonoBehaviour
{

    private List<GameObject> roadPieces = new List<GameObject>(); //Used for single mesh generation
    private List<GameObject> outerPieces = new List<GameObject>(); //Used for single mesh generation
    private List<GameObject> innerPieces = new List<GameObject>(); //Used for single mesh generation


    private List<Vector3> points = new List<Vector3>(); //All points of the spline

    private List<Vector3> inner = new List<Vector3>(); //All points of the spline
    private List<Vector3> outer = new List<Vector3>(); //All points of the spline



    private Vector3[] CurveCoordinates;
    private Vector3[] Tangents;

    public List<GameObject> Points;
    public int CurveResolution = 10;
    public float extrude;
    public float edgeWidth;
    public float thickness;

    public bool debug = true;

    private bool ClosedLoop = true;


    void Update()
    {
        DrawSpline(false);
    }

    public void DrawSpline (bool store)
    {
        if (store)
        {
            points.Clear();
            inner.Clear();
            outer.Clear();
        }

        Vector3 p0;
        Vector3 p1;
        Vector3 m0;
        Vector3 m1;

        int pointsToMake;

        if (ClosedLoop == true)
        {
            pointsToMake = (CurveResolution) * (Points.Count);
        }
        else
        {
            pointsToMake = (CurveResolution) * (Points.Count - 1);
        }

        if (pointsToMake > 0) //Prevent Number Overflow
        {
            CurveCoordinates = new Vector3[pointsToMake];
            Tangents = new Vector3[pointsToMake];

            int closedAdjustment = ClosedLoop ? 0 : 1;

            // First for loop goes through each individual control point and connects it to the next, so 0-1, 1-2, 2-3 and so on
            for (int i = 0; i < Points.Count - closedAdjustment; i++)
            {
                p0 = Points[i].transform.position;
                p1 = (ClosedLoop == true && i == Points.Count - 1) ? Points[0].transform.position : Points[i + 1].transform.position;

                // Tangent calculation for each control point
                // Tangent M[k] = (P[k+1] - P[k-1]) / 2
                // With [] indicating subscript

                // m0
                if (i == 0)
                {
                    m0 = ClosedLoop ? 0.5f * (p1 - Points[Points.Count - 1].transform.position) : p1 - p0;
                }
                else
                {
                    m0 = 0.5f * (p1 - Points[i - 1].transform.position);
                }

                // m1
                if (ClosedLoop)
                {
                    if (i == Points.Count - 1)
                    {
                        m1 = 0.5f * (Points[(i + 2) % Points.Count].transform.position - p0);
                    }
                    else if (i == 0)
                    {
                        m1 = 0.5f * (Points[i + 2].transform.position - p0);
                    }
                    else
                    {
                        m1 = 0.5f * (Points[(i + 2) % Points.Count].transform.position - p0);
                    }
                }
                else
                {
                    if (i < Points.Count - 2)
                    {
                        m1 = 0.5f * (Points[(i + 2) % Points.Count].transform.position - p0);
                    }
                    else
                    {
                        m1 = p1 - p0;
                    }
                }

                Vector3 position;
                float t;
                float pointStep = 1.0f / CurveResolution;

                if ((i == Points.Count - 2 && ClosedLoop == false) || (i == Points.Count - 1 && ClosedLoop))
                {
                    pointStep = 1.0f / (CurveResolution - 1);
                    // last point of last segment should reach p1
                }
                // Second for loop actually creates the spline for this particular segment
                for (int j = 0; j < CurveResolution; j++)
                {
                    t = j * pointStep;
                    Vector3 tangent;
                    position = CatmullRom.Interpolate(p0, p1, m0, m1, t, out tangent);
                    CurveCoordinates[i * CurveResolution + j] = position;
                    Tangents[i * CurveResolution + j] = tangent;

                    if (debug) //Normals
                    {
                        Debug.DrawLine(position + Vector3.Cross(tangent, Vector3.up).normalized * extrude / 2 + transform.position, position - Vector3.Cross(tangent, Vector3.up).normalized * extrude / 2 + transform.position, Color.red);
                        Debug.DrawLine(position + Vector3.Cross(tangent, Vector3.up).normalized * extrude / 2 + transform.position, position + Vector3.Cross(tangent, Vector3.up).normalized * (extrude / 2 + edgeWidth) + transform.position, Color.green); //Edge +
                        Debug.DrawLine(position - Vector3.Cross(tangent, Vector3.up).normalized * extrude / 2 + transform.position, position - Vector3.Cross(tangent, Vector3.up).normalized * (extrude / 2 + edgeWidth) + transform.position, Color.green); //Edge -
                    }

                    if (store)
                    {
                        points.Add(position - Vector3.Cross(tangent, Vector3.up).normalized * extrude / 2);
                        points.Add(position);
                        points.Add(position + Vector3.Cross(tangent, Vector3.up).normalized * extrude / 2);

                        inner.Add(position - Vector3.Cross(tangent, Vector3.up).normalized * extrude / 2);
                        inner.Add(position - Vector3.Cross(tangent, Vector3.up).normalized * (extrude / 2 + edgeWidth));

                        outer.Add(position + Vector3.Cross(tangent, Vector3.up).normalized * extrude / 2);
                        outer.Add(position + Vector3.Cross(tangent, Vector3.up).normalized * (extrude / 2 + edgeWidth));
                    }

                }
            }

            for (int i = 0; i < CurveCoordinates.Length - 1; ++i)
            {
                Debug.DrawLine(CurveCoordinates[i] + transform.position, CurveCoordinates[i + 1] + transform.position, Color.cyan);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (debug)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < CurveCoordinates.Length; i++)
            {
                Gizmos.DrawWireCube(CurveCoordinates[i] + transform.position, new Vector3(.1f, .1f, .1f));
            }
        }
    }

  
    public void GenerateMesh ()
    {

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).gameObject.name != "Control Points")
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        innerPieces.Clear();
        outerPieces.Clear();
        roadPieces.Clear();

        int[] previous = new int[3];

        for (int i = 0; i < (CurveResolution * Points.Count) - 1; i++)
        {
            List<Vector3> pointarinos = new List<Vector3>();
            pointarinos.Clear();

                if (i == 0)
                {
                    pointarinos.Add(points[0]);
                    pointarinos.Add(points[1]);
                    pointarinos.Add(points[2]);
                    pointarinos.Add(points[3]);
                    pointarinos.Add(points[4]);
                    pointarinos.Add(points[5]);

                    previous = new int[]
                    {
                        3, 4, 5
                    };
                }
                else if (i < points.Count - 3)
                {
                    pointarinos.Add(points[previous[0]]);
                    pointarinos.Add(points[previous[1]]);
                    pointarinos.Add(points[previous[2]]);
                    pointarinos.Add(points[previous[2] + 1]);
                    pointarinos.Add(points[previous[2] + 2]);
                    pointarinos.Add(points[previous[2] + 3]);

                    previous = new int[]
                    {
                        previous[2] + 1, previous[2] + 2, previous[2] + 3
                    };
                } 
                else
                {
                    pointarinos.Add(points[points.Count - 2]);
                    pointarinos.Add(points[points.Count - 1]);
                    pointarinos.Add(points[points.Count]);
                    pointarinos.Add(points[0]);
                    pointarinos.Add(points[1]);
                    pointarinos.Add(points[2]);
                }
           
            List<Vector3> lower = new List<Vector3>(pointarinos);
                                  
            for (int z = 0; z < lower.Count; z++)
            {
                lower[z] -= new Vector3(0, thickness, 0);
            }

            pointarinos.AddRange(lower);

            Mesh mesh = new Mesh();

            mesh = MeshMaker.MeshFromPoints(pointarinos);

            GameObject obj = new GameObject();

            MeshFilter filter = obj.AddComponent<MeshFilter>();
            MeshRenderer rend = obj.AddComponent<MeshRenderer>();

            rend.sharedMaterial = new Material(Shader.Find("Standard"));
            rend.sharedMaterial.color = Color.black;
            filter.sharedMesh = LowPolyConverter.Convert(mesh);
            
            roadPieces.Add(obj);
        }

        Mesh combinedMesh = CombineMeshes.Combine(roadPieces);

        GetComponent<MeshFilter>().sharedMesh = combinedMesh;

        foreach (GameObject obj in roadPieces)
        {
            DestroyImmediate(obj);
        }

        DrawInner();
        DrawOuter();

    }


    private void DrawInner()
    {
        Mesh mesh = EdgeExtruder.LinearMesh(inner, CurveResolution * Points.Count, thickness);

        GameObject piece = new GameObject("Inner Edge");

        piece.transform.SetParent(transform);
        piece.transform.position = transform.position;

        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer rend = piece.AddComponent<MeshRenderer>();

        rend.sharedMaterial = new Material(Shader.Find("Standard"));
        rend.sharedMaterial.color = Color.white;
    }

    private void DrawOuter()
    {
        Mesh mesh = EdgeExtruder.LinearMesh(outer, CurveResolution * Points.Count, thickness);

        GameObject piece = new GameObject("Outer Edge");

        piece.transform.SetParent(transform);
        piece.transform.position = transform.position;

        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer rend = piece.AddComponent<MeshRenderer>();

        rend.sharedMaterial = new Material(Shader.Find("Standard"));
        rend.sharedMaterial.color = Color.white;
    }
}

