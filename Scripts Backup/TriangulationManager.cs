using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using System.Collections.Generic;

public struct Edge
{
    public Vector2 A, B;

    public Edge(Vector2 a, Vector2 b)
    {
        A = a;
        B = b;
    }

    public bool Compare(Edge edge)
    {
        return A == edge.A && B == edge.B || B == edge.A && A == edge.B;
    }
}

public class Triangle
{
    public Vector2 pointA, pointB, pointC;
    public Dictionary<Edge, Triangle> links;
    public float circumRadius;
    public Vector2 circumCenter;
    public bool active;

    public Triangle(Vector2 A, Vector2 B, Vector2 C, Triangle L0 = null, Triangle L1 = null, Triangle L2 = null, bool Active = false)
    {
        active = Active;
        pointA = A;
        // Order the vertices counterclock-wise, using the cross product's z polarity.
        SortCCW(B, C);

        // Calculate the circumscribed circle to draw.
        CalculateCircumscribedCircle();

        // Links management
        links = new Dictionary<Edge, Triangle>();
        links.Add(new Edge(A, B), null);
        links.Add(new Edge(B, C), null);
        links.Add(new Edge(C, A), null);

        addLink(L0);
        addLink(L1);
        addLink(L2);
    }

    public Triangle()
    {
        pointA = new Vector2(0f, 0f);
        pointB = new Vector2(0f, 0f);
        pointC = new Vector2(0f, 0f);
        links = new Dictionary<Edge, Triangle>();
    }

    public void CalculateCircumscribedCircle()
    {
        // first calculate the midpoint of two out of three edges
        Vector2 midAB = new Vector2((pointA.x + pointB.x) / 2, (pointA.y + pointB.y) / 2);
        Vector2 midAC = new Vector2((pointA.x + pointC.x) / 2, (pointA.y + pointC.y) / 2);        
        // calculate the slope of each edge, then get the negative reciprocal for the perpendicular (m)
        float negRecSlopeAB = -1 / ((pointB.y - pointA.y) / (pointB.x - pointA.x));
        float negRecSlopeAC = -1 / ((pointC.y - pointA.y) / (pointC.x - pointA.x));
        float circumCenterX, circumCenterY;
        // check degenerate (vertical) line, in case two points are perfectly horizontal
        if (negRecSlopeAB == -Mathf.Infinity)
        {
            circumCenterX = midAB.x;
            float intercept = midAC.y - midAC.x * negRecSlopeAC;
            circumCenterY = negRecSlopeAC * circumCenterX + intercept;
        }
        else if (negRecSlopeAC == -Mathf.Infinity)
        {
            circumCenterX = midAC.x;
            float intercept = midAB.y - midAB.x * negRecSlopeAB;
            circumCenterY = negRecSlopeAB * circumCenterX + intercept;
        }
        else
        {
            // find the x0 intercept (q)
            float interceptAB = midAB.y - midAB.x * negRecSlopeAB;
            float interceptAC = midAC.y - midAC.x * negRecSlopeAC;
            // the equations of our perpendiculars is now found in the form: y = negRecSlope * x + intercept
            // with a system of the two equations we find x and y of their intercept, aka the center of our circumcircle
            circumCenterX = (interceptAC - interceptAB) / (negRecSlopeAB - negRecSlopeAC);
            circumCenterY = negRecSlopeAB * circumCenterX + interceptAB;
        }
        
        circumCenter = new Vector2(circumCenterX, circumCenterY);
        // find the radius with a simple vector operation
        circumRadius = (pointA - circumCenter).magnitude;
        
    }

    public void SortCCW(Vector2 B, Vector2 C)
    {
        if (Vector3.Cross(pointA - B, pointA - C).z < 0)
        {
            pointB = C; pointC = B;
        }
        else
        {
            pointB = B; pointC = C;
        }
    }

    public bool isPointInsideCircumcircle(Vector2 point)
    {
        float distance = (point - circumCenter).magnitude;

        if (distance < circumRadius)
            return true;

        return false;
    }

    public override string ToString()
    {
        return "PointA: " + pointA.ToString() + " PointB: " + pointB.ToString() + " PointC: " + pointC.ToString() +
               " CircumCenter: " + circumCenter.ToString() + " CircumRadius: " + circumRadius;
    }

    public bool sharedVertex(Triangle triangle)
    {
        bool test = pointA == triangle.pointA || pointA == triangle.pointB || pointA == triangle.pointC
                 || pointB == triangle.pointA || pointB == triangle.pointB || pointB == triangle.pointC
                 || pointC == triangle.pointA || pointC == triangle.pointB || pointC == triangle.pointC;
        
        return test;
        /*return pointA == triangle.pointA || pointA == triangle.pointB || pointA == triangle.pointC
            || pointB == triangle.pointA || pointB == triangle.pointB || pointB == triangle.pointC
            || pointC == triangle.pointA || pointC == triangle.pointB || pointC == triangle.pointC;*/
    }

    public void addLink(Triangle triangle)
    {
        if (triangle != null && triangle != this)
            foreach (Edge edge in triangle.links.Keys)
                foreach (Edge link in links.Keys)
                    if (link.Compare(edge)) 
                    { 
                        links[link] = triangle;                                                
                        break;                        
                    }
    }
}

public class TriangulationManager : MonoBehaviour
{

    public class Triangulation
    {
        int size = 0;
        int maxSize;

        public List<Triangle> _triangulation;
        

        public Triangulation(int Max = MAX_SIZE)
        {
            maxSize = Max;
            _triangulation = new List<Triangle>();
            for (int i = 0; i < maxSize; i++)
                _triangulation.Add(new Triangle());
        }

        public void Reset(int poolMinSize)
        {            
            for (int i = size; i < poolMinSize; i++)
                _triangulation.Add(new Triangle());
            print(poolMinSize - size);
            size = 0;
        }

        public Triangle At(int index)
        {            
            if (index < size) return _triangulation[index];
            return null; //should never be called
        }

        public void Remove(Triangle triangle)
        {
            _triangulation.Remove(triangle);
            size--;
        }

        public void Add(Triangle triangle)
        {
            _triangulation[size] = triangle;
            _triangulation[size].active = true;
            size++;
        }
        
        public void Add(Vector2 A, Vector2 B, Vector2 C)
        {
            _triangulation[size].active = true;
            _triangulation[size].pointA = A;
            _triangulation[size].SortCCW(B, C);

            _triangulation[size].CalculateCircumscribedCircle();

            _triangulation[size].links.Clear();
            _triangulation[size].links.Add(new Edge(A, B), null);
            _triangulation[size].links.Add(new Edge(B, C), null);
            _triangulation[size].links.Add(new Edge(C, A), null);

            size++;
        }

        public int Size()
        {
            return size;
        }
            
    }

    const int MAX_SIZE = 100000;
    public int howManyPoints;
    float halfHeight, halfWidth;
    List<GameObject> points;
    public Triangulation triangulation, partialTriangulation;
    public GameObject pointGO, pointContainerGO;    
    public float scalePlayground;
    public int triangulationAlg;
    Triangle superTriangle;

    private void Start()
    {
        halfHeight = Camera.main.orthographicSize;
        halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        points = new List<GameObject>();
        triangulation = new Triangulation();
        partialTriangulation = new Triangulation(20);
        GeneratePoints();
        Triangulate();        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GeneratePoints();

        switch (triangulationAlg)
        {
            case 0: Triangulate();     break;
            case 1: ContiguousTriangulate(); break;
        }
    }

    /*private void OnDrawGizmos()
    {
        Handles.color = Color.green;
        foreach (Triangle triangle in triangulation)
            Handles.DrawWireDisc(triangle.circumCenter, Vector3.forward, triangle.circumRadius);
    }*/

    public void OnChangeAmount(int value)
    {
        howManyPoints = value;
        GeneratePoints();
    }

    public void OnChangeBackgroundColor(Color color)
    {
        Camera.main.backgroundColor = color;
    }

    public void OnChangeShowPoints(bool value)
    {
        for (int i = 0; i < pointContainerGO.transform.childCount; i++)
        {
            points[i].GetComponent<SpriteRenderer>().enabled = value;
        }
        
    }

    private void GeneratePoints()
    {    
        for (int i = 0; i < pointContainerGO.transform.childCount; i++)
        {
            Destroy(points[i]);
        }

        points = new List<GameObject>();

        //Random
        for (int i = 0; i < howManyPoints; i++)
        {            
            GameObject point = Instantiate(pointGO,
                                            new Vector2(Random.Range(-halfWidth  * scalePlayground, halfWidth  * scalePlayground),
                                                        Random.Range(-halfHeight * scalePlayground, halfHeight * scalePlayground)), 
                                            Quaternion.identity,
                                            pointContainerGO.transform);
            points.Add(point);
        }
    }

    private void Triangulate()
    {
        triangulation.Reset(howManyPoints*4);
        superTriangle = new Triangle(new Vector2(-halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(0.0f, halfHeight * 3 * scalePlayground));

        triangulation.Add(superTriangle);

        for (int i=0; i < howManyPoints; i++)
        {
            Vector2 point = pointContainerGO.transform.GetChild(i).transform.position;

            Profiler.BeginSample("Triangles finder");

            List<Triangle> badTriangles = new List<Triangle>();
            for (int j = 0; j < triangulation.Size(); j++)
                if (triangulation.At(j).isPointInsideCircumcircle(point))
                    badTriangles.Add(triangulation.At(j));
            Profiler.EndSample();

            Profiler.BeginSample("Polygon Creation");

            List<Edge> polygon = new List<Edge>();
            for (int j = 0; j < badTriangles.Count; j++) // triangles
            {
                foreach (Edge edge in badTriangles[j].links.Keys) // edges            
                {
                    bool isShared = false;
                    for (int l = 0; l < badTriangles.Count; l++) // triangles
                    {
                        //if (badTriangles[l] != badTriangles[j] && !isShared)
                        if (l != j && !isShared)
                        {
                            foreach (Edge badEdge in badTriangles[l].links.Keys)
                                if (edge.Compare(badEdge))
                                    isShared = true;
                        }                        
                    }
                    if (!isShared)
                        polygon.Add(edge);
                }
            }
            Profiler.EndSample();
            Profiler.BeginSample("Removal");
            for (int j = 0; j < badTriangles.Count; j++)
                triangulation.Remove(badTriangles[j]);
            Profiler.EndSample();

            Profiler.BeginSample("Allocation");
            for (int j = 0; j < polygon.Count; j++)
                triangulation.Add(polygon[j].A, polygon[j].B, point);
            Profiler.EndSample();
        }
        
        /*int j = 0;
        while (j < triangulation.Count)
        {
            if (superTriangle.sharedVertex(triangulation[j]))
                triangulation.RemoveAt(j);
            else j++;
        }*/
    }

    private void ContiguousTriangulate()
    {
        void checkEdges(ref List<Triangle> badTriangles, Vector2 point, Triangle triangle)
        {
            foreach(Edge edge in triangle.links.Keys)
            {
                if (triangle.links[edge] != null && triangle.links[edge].isPointInsideCircumcircle(point) &&
                    !badTriangles.Contains(triangle.links[edge]))
                {
                    badTriangles.Add(triangle.links[edge]);
                    checkEdges(ref badTriangles, point, triangle.links[edge]);
                }
            }
        }

        triangulation.Reset(howManyPoints * 10);
        superTriangle = new Triangle(new Vector2(-halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(0.0f, halfHeight * 3 * scalePlayground));

        triangulation.Add(superTriangle);

        for (int i = 0; i < howManyPoints; i++)
        {
            Profiler.BeginSample("Finding Bad Triangles");
            Vector2 point = pointContainerGO.transform.GetChild(i).transform.position;
            List<Triangle> badTriangles = new List<Triangle>();            

            for (int j = triangulation.Size()-1; j >= 0; j--)
                if (triangulation.At(j).isPointInsideCircumcircle(point))
                {
                    badTriangles.Add(triangulation.At(j));
                    checkEdges(ref badTriangles, point, triangulation.At(j));
                    break;
                }

            Profiler.EndSample();

            Profiler.BeginSample("Allocation");
            List<Edge> polygon = new List<Edge>();
            List<Triangle> outerTriangles = new List<Triangle>();
            Profiler.EndSample();

            Profiler.BeginSample("Finding Polygon");

            for (int j=0; j < badTriangles.Count; j++)
            {
                foreach (Edge edge in badTriangles[j].links.Keys)
                {
                    bool isShared = false;
                    for (int k=0; k < badTriangles.Count; k++)
                    {
                        //if (badTriangle != badTriangles[j] && !isShared)
                        if (k != j && !isShared)
                        {
                            foreach (Edge badEdge in badTriangles[k].links.Keys)
                            { 
                                if (edge.Compare(badEdge))
                                {
                                    isShared = true;
                                    break;
                                }
                                
                            }
                        }
                    }
                    if (!isShared)
                    {
                        polygon.Add(edge);
                        if (point != edge.A && point != edge.B && badTriangles[j].links[edge] != null &&
                                !outerTriangles.Contains(badTriangles[j].links[edge]))
                            outerTriangles.Add(badTriangles[j].links[edge]);
                    }
                }                
            }

            Profiler.EndSample();

            Profiler.BeginSample("Removing bad Triangles");
            for (int j = 0; j < badTriangles.Count; j++)
                triangulation.Remove(badTriangles[j]);
            Profiler.EndSample();

            partialTriangulation.Reset(20);
            Profiler.BeginSample("Allocation partial");
            for (int j = 0; j < polygon.Count; j++)
                partialTriangulation.Add(new Triangle(polygon[j].A, polygon[j].B, point));
            Profiler.EndSample();

            Profiler.BeginSample("Add triangle to triangulation");
            for (int j = 0; j < partialTriangulation.Size(); j++)
            {
                for (int k = 0; k < outerTriangles.Count; k++)
                {
                    partialTriangulation.At(j).addLink(outerTriangles[k]);
                    outerTriangles[k].addLink(partialTriangulation.At(j));
                }
                
                for (int k = 0; k < partialTriangulation.Size(); k++)
                    partialTriangulation.At(k).addLink(partialTriangulation.At(k));

                triangulation.Add(partialTriangulation.At(j));                
            }
            Profiler.EndSample();
        }

        

        /*int j = 0;
        while (j < triangulation.Count)
        {
            if (superTriangle.sharedVertex(triangulation[j]))
                triangulation.RemoveAt(j);
            else j++;
        }*/


    }
}
