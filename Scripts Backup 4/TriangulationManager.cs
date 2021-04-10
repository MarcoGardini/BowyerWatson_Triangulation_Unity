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
    public List<Edge>     edges; // VVV
    public List<Triangle> links; // ^^^
    public float circumRadius;
    public Vector2 circumCenter;    
    public int poolIndex;

    public Triangle(int index)
    {
        pointA = new Vector2(0f, 0f);
        pointB = new Vector2(0f, 0f);
        pointC = new Vector2(0f, 0f);
        edges  = new List<Edge>();
        links  = new List<Triangle>();

        edges.Add(new Edge(pointA, pointB));
        links.Add(null);
        edges.Add(new Edge(pointB, pointC));
        links.Add(null);
        edges.Add(new Edge(pointC, pointA));
        links.Add(null);

        poolIndex = index;
    }

    public void Populate(Vector2 A, Vector2 B, Vector2 C)
    {
        pointA = A;
        SortCCW(B, C);

        CalculateCircumscribedCircle();

        edges.Clear();
        edges.Add(new Edge(pointA, pointB));
        edges.Add(new Edge(pointB, pointC));
        edges.Add(new Edge(pointC, pointA));
        links[0] = null;
        links[1] = null;
        links[2] = null;
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
        return (point - circumCenter).magnitude < circumRadius;       
    }

    public override string ToString()
    {
        return "PointA: " + pointA.ToString() + " PointB: " + pointB.ToString() + " PointC: " + pointC.ToString() +
               " CircumCenter: " + circumCenter.ToString() + " CircumRadius: " + circumRadius;
    }

     public Vector2 sharedVertex(Triangle triangle)
    {
        if (pointA == triangle.pointA || pointA == triangle.pointB || pointA == triangle.pointC)
            return pointA;
        if (pointB == triangle.pointA || pointB == triangle.pointB || pointB == triangle.pointC)
            return pointB;
        if (pointC == triangle.pointA || pointC == triangle.pointB || pointC == triangle.pointC)
            return pointC;
        return new Vector2(-6000f, -6000f);
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
            trianglePool.Add(new Triangle(i));
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
        inactiveTrianglesIndices.AddLast(triangle.poolIndex);
    }

    public void Clear()
    {
        inactiveTrianglesIndices.Clear();
        currentSize = 0;
    }
}

public class Point
{
    public float speed = 0.2f;
    public Vector2 position, direction;
    public const int changeDirection = 1000;
    public const float maxDistanceFromSpawn = 13.0f;

    public Point()
    {
        position = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }

    void Update()
    {        
        position += direction * speed * Time.deltaTime;
        if (Random.Range(0, changeDirection) == 0)
            direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        if (position.magnitude >= maxDistanceFromSpawn)
            direction = -direction;
    }
}


public class TriangulationManager : MonoBehaviour
{    
    public int howManyPoints;
    float halfHeight, halfWidth;
    List<GameObject> points;
    List<Point> newPoints;
    public TrianglePool trianglePool;
    public HashSet<Triangle> triangulation;
    public GameObject pointGO, pointContainerGO;    
    public float scalePlayground;
    public int triangulationAlg;
    Triangle superTriangle;

    private void Start()
    {
        halfHeight = Camera.main.orthographicSize;
        halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        points = new List<GameObject>();
        newPoints = new List<Point>();
        trianglePool = new TrianglePool();
        triangulation = new HashSet<Triangle>();
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
        newPoints = new List<Point>();

        //Random
        for (int i = 0; i < howManyPoints; i++)
        {            
            GameObject point = Instantiate(pointGO,
                                            new Vector2(Random.Range(-halfWidth  * scalePlayground, halfWidth  * scalePlayground),
                                                        Random.Range(-halfHeight * scalePlayground, halfHeight * scalePlayground)), 
                                            Quaternion.identity,
                                            pointContainerGO.transform);
            points.Add(point);

            newPoints.Add(new Point());
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

        //for (int i = 0; i < howManyPoints; i++)
        //{
        for (int i = 0; i < newPoints.Count; i++)
        { 
            //Vector2 point = pointContainerGO.transform.GetChild(i).transform.position;
            Vector2 point = newPoints[i].position;
            badTriangles.Clear();

            Profiler.BeginSample("Triangles finder");
            foreach(Triangle triangle in triangulation)            
                if (triangle.isPointInsideCircumcircle(point))
                    badTriangles.Add(triangle);
            Profiler.EndSample();

            Profiler.BeginSample("Polygon Creation");
            polygon.Clear();

            for (int outer = 0; outer < badTriangles.Count; outer++) // triangles
            {
                for (int edge = 0; edge < 3; edge++)           
                {
                    bool isShared = false;
                    for (int inner = 0; inner < badTriangles.Count; inner++) // triangles
                    {                        
                        if (inner != outer && !isShared)
                        {   
                            for (int badEdge = 0; badEdge < 3; badEdge++)
                                if (badTriangles[outer].edges[edge].Compare(badTriangles[inner].edges[badEdge]))
                                    isShared = true;
                        }                        
                    }
                    if (!isShared)
                        polygon.Add(badTriangles[outer].edges[edge]);
                }
            }

            Profiler.EndSample();            
            for (int j = 0; j < badTriangles.Count; j++)
            {
                Profiler.BeginSample("Removal");
                trianglePool.Remove(badTriangles[j]);
                Profiler.EndSample();
                triangulation.Remove(badTriangles[j]);
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
    }

    private void ContiguousTriangulate()
    {
        void checkEdges(HashSet<Triangle> _badTriangles, Vector2 point, Triangle triangle)
        {
            for (int edge = 0; edge < 3; edge++)
            {
                if (triangle.links[edge] != null &&
                    triangle.links[edge].isPointInsideCircumcircle(point) &&
                    !_badTriangles.Contains(triangle.links[edge]))
                {
                    _badTriangles.Add(triangle.links[edge]);
                    checkEdges(_badTriangles, point, triangle.links[edge]);
                }
            }
        }

        bool linkTriangles(Triangle triangle1, Triangle triangle2)
        {
            if (triangle1 != triangle2 && triangle1 != null && triangle2 != null)
            {
                for (int outer = 0; outer < 3; outer++)
                    for (int inner = 0; inner < 3; inner++)
                    {
                        if (triangle1.edges[outer].Compare(triangle2.edges[inner]))
                        {
                            triangle1.links[outer] = triangle2;
                            return true;
                        }
                    }
            }
            return false;
        }

        triangulation.Clear();
        trianglePool.Clear();
        superTriangle = trianglePool.Get();
        superTriangle.Populate(new Vector2(-halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(0.0f, halfHeight * 3 * scalePlayground));

        triangulation.Add(superTriangle);
        List<Edge> polygon = new List<Edge>();
        HashSet<Triangle> badTriangles = new HashSet<Triangle>();
        HashSet<Triangle> outerTriangles = new HashSet<Triangle>();
        HashSet<Triangle> partialTriangulation = new HashSet<Triangle>();

        for (int i = 0; i < howManyPoints; i++)
        {
            Profiler.BeginSample("Finding Bad Triangles");
            Vector2 point = pointContainerGO.transform.GetChild(i).transform.position;

            polygon.Clear();
            badTriangles.Clear();
            outerTriangles.Clear();
            partialTriangulation.Clear();
            foreach (Triangle triangle in triangulation)
                if (triangle.isPointInsideCircumcircle(point))
                {
                    badTriangles.Add(triangle);
                    checkEdges(badTriangles, point, triangle);
                    break;
                }

            Profiler.EndSample();

            Profiler.BeginSample("Finding Polygon");

            foreach(Triangle badTriangle in badTriangles)
            {
                for (int edge = 0; edge < 3; edge++)
                {
                    if (!badTriangles.Contains(badTriangle.links[edge]))
                    {
                        polygon.Add(badTriangle.edges[edge]);
                        if (badTriangle.links[edge] != null &&
                            !badTriangles.Contains(badTriangle.links[edge]))
                            outerTriangles.Add(badTriangle.links[edge]);
                    }
                }
            }

            Profiler.EndSample();

            foreach (Triangle badTriangle in badTriangles)
            {
                trianglePool.Remove(badTriangle);
                Profiler.BeginSample("Removing bad Triangles");
                triangulation.Remove(badTriangle);
                Profiler.EndSample();
            }

            Profiler.BeginSample("Allocation partial");
            for (int j = 0; j < polygon.Count; j++)
            {
                Triangle thisTriangle = trianglePool.Get();
                thisTriangle.Populate(polygon[j].A, polygon[j].B, point);
                partialTriangulation.Add(thisTriangle);
            }
            Profiler.EndSample();

            Profiler.BeginSample("Add triangle to triangulation");
            foreach (Triangle partialTriangle in partialTriangulation)
            {
                Profiler.BeginSample("First");
                foreach (Triangle outerTriangle in outerTriangles)
                    if (linkTriangles(partialTriangle, outerTriangle))
                    {
                        linkTriangles(outerTriangle, partialTriangle);
                        break;
                    }

                Profiler.EndSample();

                Profiler.BeginSample("Second");
                int maxTwoLinked = 0;
                foreach (Triangle otherPartialTriangle in partialTriangulation)
                {
                    if (linkTriangles(partialTriangle, otherPartialTriangle))
                    {
                        maxTwoLinked++;
                    }
                    if (maxTwoLinked == 2) break;
                }
                Profiler.EndSample();

                triangulation.Add(partialTriangle);
            }

            Profiler.EndSample();
        }
    }
}
