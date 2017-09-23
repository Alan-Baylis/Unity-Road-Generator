using System.Collections.Generic;
using UnityEngine;

public class EdgeExtruder : MonoBehaviour
{

    public static Mesh LinearMesh(List<Vector3> points, int iterations, float thickness)
    {
        List<GameObject> objects = new List<GameObject>();

        int[] previous = new int[2];

        for (int i = 0; i < iterations - 1; i++)
        {
            List<Vector3> pointarinos = new List<Vector3>();
            pointarinos.Clear();

            if (i == 0)
            {
                pointarinos.Add(points[0]);
                pointarinos.Add(points[1]);
                pointarinos.Add(points[2]);
                pointarinos.Add(points[3]);


                previous = new int[]
                {
                        2, 3
                };
            }
            else if (i < points.Count - 2)
            {
                pointarinos.Add(points[previous[0]]);
                pointarinos.Add(points[previous[1]]);
                pointarinos.Add(points[previous[1] + 1]);
                pointarinos.Add(points[previous[1] + 2]);

                previous = new int[]
                {
                        previous[1] + 1, previous[1] + 2
                };
            }
            else
            {

                pointarinos.Add(points[points.Count - 1]);
                pointarinos.Add(points[points.Count]);
                pointarinos.Add(points[0]);
                pointarinos.Add(points[1]);

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
            obj.AddComponent<MeshRenderer>();

            filter.sharedMesh = LowPolyConverter.Convert(mesh);

            objects.Add(obj);
                        
        }

        Mesh combinedMesh = CombineMeshes.Combine(objects);
        
        foreach (GameObject obj in objects)
        {
            DestroyImmediate(obj);
        }

        return combinedMesh;
    }
	
}
