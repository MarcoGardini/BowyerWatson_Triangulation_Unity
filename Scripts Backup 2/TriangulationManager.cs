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

    public void Populate(Vector2 A, Vector2 B, Vector2 C)
    {
        pointA = A;
        SortCCW(B, C);

        CalculateCircumscribedCircle();

        links.Clear();
        links.Add(new Edge(A, B), null);
        links.Add(new Edge(B, C), null);
        links.Add(new Edge(C, A), null);
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
        return pointA == triangle.pointA || pointA == triangle.pointB || pointA == triangle.pointC
            || pointB == triangle.pointA || pointB == triangle.pointB || pointB == triangle.pointC
            || pointC == triangle.pointA || pointC == triangle.pointB || pointC == triangle.pointC;
    }

    public void addLink(Triangle triangle)
    {
        if (triangle != null && triangle != this)
            foreach (Edge link in links.Keys)
                foreach (Edge edge in triangle.links.Keys)
                    if (link.Compare(edge)) 
                    { 
                        links[link] = triangle;
                        triangle.links[edge] = this;
                        return;            
                    }
    }
}

public class TrianglePool
{
    const int MAX_SIZE = 100000;
    public int currentSize;
    public List<Triangle> trianglePool;
    public LinkedList<int> inactiveTrianglesIndices;

    public TrianglePool()
    {
        currentSize = 0;
        trianglePool = new List<Triangle>();
        for (int i = 0; i < MAX_SIZE; i++)
            trianglePool.Add(new Triangle());
        inactiveTrianglesIndices = new LinkedList<int>();
    }

    public Triangle At(int index) // shouldn't be needed
    {
        return trianglePool[index];
    }

    public Triangle Get()
    {
        Triangle thisTriangle;
        if (inactiveTrianglesIndices.Count > 0)
        {
            thisTriangle = trianglePool[inactiveTrianglesIndices.First.Value];
            inactiveTrianglesIndices.RemoveFirst();            
        }
        else
        {
            thisTriangle = trianglePool[currentSize];
            currentSize++;
        }
        return thisTriangle;
    }

    public void Remove(Triangle triangle)
    {
        for (int i = currentSize -1 ; i >= 0; i--)
            if (triangle == trianglePool[i])
            {
                inactiveTrianglesIndices.AddLast(i);
                return;
            }
    }

    public void Clear()
    {
        inactiveTrianglesIndices.Clear();
        currentSize = 0;
    }
}

public class TriangulationManager : MonoBehaviour
{
    
    public int howManyPoints;
    float halfHeight, halfWidth;
    List<GameObject> points;
    public TrianglePool trianglePool;
    public List<Triangle> triangulation;
    public List<Triangle> partialTriangulation;
    public GameObject pointGO, pointContainerGO;    
    public float scalePlayground;
    public int triangulationAlg;
    Triangle superTriangle;

    private void Start()
    {
        halfHeight = Camera.main.orthographicSize;
        halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        points = new List<GameObject>();
        trianglePool = new TrianglePool();
        triangulation = new List<Triangle>();
        partialTriangulation = new List<Triangle>();
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
        triangulation.Clear();
        trianglePool.Clear();
        superTriangle = trianglePool.Get();
        superTriangle.Populate(new Vector2(-halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(0.0f, halfHeight * 3 * scalePlayground));

        triangulation.Add(superTriangle);

        List<Triangle> badTriangles = new List<Triangle>();
        List<Edge> polygon = new List<Edge>();

        for (int i=0; i < howManyPoints; i++)
        {
            Vector2 point = pointContainerGO.transform.GetChild(i).transform.position;
            badTriangles.Clear();
            Profiler.BeginSample("Triangles finder");
            
            for (int j = 0; j < triangulation.Count; j++)
                if (triangulation[j].isPointInsideCircumcircle(point))
                    badTriangles.Add(triangulation[j]);
            Profiler.EndSample();

            Profiler.BeginSample("Polygon Creation");
            polygon.Clear();
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
            for (int j = 0; j < badTriangles.Count; j++)
            {
                triangulation.Remove(badTriangles[j]);
                Profiler.BeginSample("Removal");
                trianglePool.Remove(badTriangles[j]);
                Profiler.EndSample();
            }
            

            Profiler.BeginSample("Allocation");
            for (int j = 0; j < polygon.Count; j++)
            {
                Triangle thisTriangle = trianglePool.Get();
                thisTriangle.Populate(polygon[j].A, polygon[j].B, point);
                triangulation.Add(thisTriangle);
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
    
    private void ContiguousTriangulate()
    {
        void checkEdges(ref List<Triangle> _badTriangles, Vector2 point, Triangle triangle)
        {
            foreach (Edge edge in triangle.links.Keys)
            {
                if (triangle.links[edge] != null && triangle.links[edge].isPointInsideCircumcircle(point) &&
                    !_badTriangles.Contains(triangle.links[edge]))
                {
                    _badTriangles.Add(triangle.links[edge]);
                    checkEdges(ref _badTriangles, point, triangle.links[edge]);
                }
            }
        }

        triangulation.Clear();
        trianglePool.Clear();
        superTriangle = trianglePool.Get();
        superTriangle.Populate(new Vector2(-halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(0.0f, halfHeight * 3 * scalePlayground));

        triangulation.Add(superTriangle);
        List<Edge> polygon = new List<Edge>();
        List<Triangle> badTriangles = new List<Triangle>();
        List<Triangle> outerTriangles = new List<Triangle>();

        for (int i = 0; i < howManyPoints; i++)
        {
            //Profiler.BeginSample("Finding Bad Triangles");
            Vector2 point = pointContainerGO.transform.GetChild(i).transform.position;

            badTriangles.Clear();
            for (int j = triangulation.Count - 1; j >= 0; j--)
                if (triangulation[j].isPointInsideCircumcircle(point))
                {
                    badTriangles.Add(triangulation[j]);
                    Profiler.BeginSample("Am I?");
                    checkEdges(ref badTriangles, point, triangulation[j]);
                    Profiler.EndSample();
                    break;
                }

            //Profiler.EndSample();

            Profiler.BeginSample("Allocation");
            polygon.Clear();
            outerTriangles.Clear();
            Profiler.EndSample();

            Profiler.BeginSample("Finding Polygon");

            for (int j = 0; j < badTriangles.Count; j++)
            {
                foreach (Edge edge in badTriangles[j].links.Keys)
                {
                    bool isShared = false;
                    for (int k = 0; k < badTriangles.Count; k++)
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

            partialTriangulation.Clear();
            Profiler.BeginSample("Allocation partial");
            for (int j = 0; j < polygon.Count; j++)
            {
                Triangle thisTriangle = trianglePool.Get();
                thisTriangle.Populate(polygon[j].A, polygon[j].B, point);
                partialTriangulation.Add(thisTriangle);
            }
            Profiler.EndSample();

            //Profiler.BeginSample("Add triangle to triangulation");
            for (int j = 0; j < partialTriangulation.Count; j++)
            {
                Profiler.BeginSample("First");
                for (int k = 0; k < outerTriangles.Count; k++)
                {
                    partialTriangulation[j].addLink(outerTriangles[k]);
                }
                Profiler.EndSample();

                Profiler.BeginSample("Second");
                for (int k = 0; k < partialTriangulation.Count; k++)
                    partialTriangulation[j].addLink(partialTriangulation[k]);
                Profiler.EndSample();

                Profiler.BeginSample("Third");
                triangulation.Add(partialTriangulation[j]);
                Profiler.EndSample();
            }
            //Profiler.EndSample();
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
