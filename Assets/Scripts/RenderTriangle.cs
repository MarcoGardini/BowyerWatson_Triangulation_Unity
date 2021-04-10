using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// class to manage the rendering of the triangulation
public class RenderTriangle : MonoBehaviour
{
    public enum ColorTechnique {
        CTaverageDistanceFromCenter,
        CTaverageY,
        CTaverageVerticalBalance,
        CTtriangleArea,
        CTrandom,
        CTtotal,
    };

    public TriangulationManager triangulationManager;
    public ColorTechnique colorTechnique;
    public Color color;
    public bool calculatedAlpha;
    public bool invertColors;

    Mesh mesh;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        triangulationManager = FindObjectOfType<TriangulationManager>();
        colorTechnique = ColorTechnique.CTaverageDistanceFromCenter;
        color = Color.white;
        calculatedAlpha = false;
    }

    void Update()
    {               
        // prepare mesh lists
        List<Vector2> vertices = new List<Vector2>();
        List<int>     indices  = new List<int>();
        List<Color>   colors   = new List<Color>();
        
        foreach (Triangle triangle in triangulationManager.triangulation)
        {
            // add vertices
            vertices.Add(triangle.pointA); vertices.Add(triangle.pointB); vertices.Add(triangle.pointC);
            // add last three vertices' indices (defining the triangle)
            indices.Add(vertices.Count - 3); indices.Add(vertices.Count - 2); indices.Add(vertices.Count - 1);
            // based on the GUI evaluate the color technique, then add the resulting color once for each vertex       
            Color triangleColor;            

            float colorFactor = 0.0f;
            switch (colorTechnique)
            {   
                case ColorTechnique.CTaverageDistanceFromCenter:
                    colorFactor = Mathf.Abs(triangle.pointA.magnitude + triangle.pointB.magnitude + triangle.pointC.magnitude) / 
                                                     (3 * (Camera.main.orthographicSize * 4));                    
                    break;
                case ColorTechnique.CTaverageY:
                    colorFactor = (triangle.pointA.y + triangle.pointB.y + triangle.pointC.y) / 
                                     (3.0f * (Camera.main.orthographicSize * 2)) + 0.5f;
                    break;
                case ColorTechnique.CTaverageVerticalBalance:
                    colorFactor = findVerticalBalance(triangle); 
                    break;
                case ColorTechnique.CTtriangleArea:
                    colorFactor = Mathf.Abs(triangle.pointA.x * (triangle.pointB.y - triangle.pointC.y) +
                                           triangle.pointB.x * (triangle.pointC.y - triangle.pointA.y) +
                                           triangle.pointC.x * (triangle.pointA.y - triangle.pointB.y)) / 2;
                    // scale the area so that a full white triangle is one tenth of the total area
                    colorFactor /= Camera.main.orthographicSize * Camera.main.orthographicSize * Camera.main.aspect / 10; 
                    break;
                default:
                    Random.InitState((int)((triangle.pointA.x + triangle.pointB.x + triangle.pointC.x +
                                     triangle.pointA.y + triangle.pointB.y + triangle.pointC.y) * 100));
                    colorFactor = Random.Range(0f, 1f);
                    break;
            }

            // check invert colors
            colorFactor = invertColors ? 1 - colorFactor : colorFactor;
                        
            triangleColor = new Color(color.r * colorFactor, color.g * colorFactor, color.b * colorFactor, color.a * (calculatedAlpha ? colorFactor : 1.0f));
            
            colors.Add(triangleColor); colors.Add(triangleColor); colors.Add(triangleColor); 
        }

        mesh.Clear();
        mesh.vertices = System.Array.ConvertAll<Vector2, Vector3>(vertices.ToArray(), v => v);
        mesh.triangles = indices.ToArray();
        mesh.colors = colors.ToArray();
    }
        
    float findVerticalBalance(Triangle triangle)
    {
        void Swap(ref float f1, ref float f2)
        {
            float temp = f1;
            f1 = f2;
            f2 = temp;
        }

        float highest = triangle.pointA.y, lowest = triangle.pointB.y, balance = triangle.pointC.y;
        // find max
        if (lowest  > highest) Swap(ref lowest,  ref highest);
        if (balance > highest) Swap(ref balance, ref highest);
        // find min
        if (highest < lowest) Swap(ref lowest,  ref highest);
        if (balance < lowest) Swap(ref balance, ref lowest);
        // highest = 1, lowest = 0, balance is the resulting weight
        balance = (highest - balance) / (highest - lowest);

        return balance;
    }

    // called by UI
    public void OnChangeColor(Color newColor)
    {
        color = newColor;
    }

    // called by UI
    public void OnChangeCalculatedAlpha(bool value)
    {
        calculatedAlpha = value;
    }

    // called by UI
    public void OnChangeInvertColors(bool value)
    {
        invertColors = value;
    }
}
