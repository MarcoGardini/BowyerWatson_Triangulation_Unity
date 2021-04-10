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
    public List<Edge>     edges; // links VVV
    public List<Triangle> links; // edges ^^^
    public float circumRadius;
    public Vector2 circumCenter;
    public int poolIndex; 

    // constructor
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

    // set points and edges, links will be set only if needed
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

    // calculate center and radius of the triangle's circumcircle
    public void CalculateCircumscribedCircle()
    {
        // first calculate the midpoint of two out of three edges
        Vector2 midAB = new Vector2((pointA.x + pointB.x) / 2, (pointA.y + pointB.y) / 2);
        Vector2 midAC = new Vector2((pointA.x + pointC.x) / 2, (pointA.y + pointC.y) / 2);        
        // calculate the slope of each edge, then get the negative reciprocal for the perpendicular (m)
        float slopeAB = (pointB.y - pointA.y) / (pointB.x - pointA.x);
        float slopeAC = (pointC.y - pointA.y) / (pointC.x - pointA.x);        
        // check degenerate (vertical) line, in case two points are exactly horizontal
        if (slopeAB == 0.0f)
        {
            circumCenter.x = midAB.x;
            float intercept = midAC.y - midAC.x * (-1 / slopeAC); // negative reciprocal
            circumCenter.y = (-1 / slopeAC) * circumCenter.x + intercept;
        }
        else if (slopeAC == 0.0f)
        {
            circumCenter.x = midAC.x;
            float intercept = midAB.y - midAB.x * (-1 / slopeAB); // negative reciprocal
            circumCenter.y = (-1 / slopeAB) * circumCenter.x + intercept;
        }
        else
        {
            // find the x0 intercept (q), using the negative reciprocal of m
            float interceptAB = midAB.y + midAB.x / slopeAB;
            float interceptAC = midAC.y + midAC.x / slopeAC;
            // the equations of our perpendiculars is now found in the form: y = negRecSlope * x + intercept
            // with a system of the two equations we find x and y of their intercept, aka the center of our circumcircle
            circumCenter.x = (interceptAC - interceptAB) / (-1 / slopeAB + 1 / slopeAC);
            circumCenter.y = -circumCenter.x / slopeAB + interceptAB;
        }        
        // find the radius with a simple vector operation
        circumRadius = (pointA - circumCenter).magnitude;      
    }

    // sort counter-clock wise
    public void SortCCW(Vector2 B, Vector2 C)
    {
        // find "front face" with cross product sign
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
}

public class TrianglePool
{
    const int MAX_SIZE = 100000;
    public int currentSize;
    public List<Triangle> trianglePool;
    // avoid fragmentation by saving inactive triangles in a linked list
    public LinkedList<int> inactiveTrianglesIndices;

    // constructor
    public TrianglePool()
    {
        currentSize = 0;
        trianglePool = new List<Triangle>();
        for (int i = 0; i < MAX_SIZE; i++)
            trianglePool.Add(new Triangle(i));
        inactiveTrianglesIndices = new LinkedList<int>();
    }

    // returns a new usable triangle
    public Triangle Get()
    {       
        Triangle thisTriangle;
        // return an inactive triangle if there is any, then a new one
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

    // render a triangle inactive by adding it to the linked list
    public void Remove(Triangle triangle)
    {        
        inactiveTrianglesIndices.AddLast(triangle.poolIndex);
    }

    // reset the pool
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
    public const float maxDistanceFromSpawn = 10.0f;

    public Point(float horizontalHalfRange, float verticalHalfRange)
    {
        position = new Vector2(Random.Range(-horizontalHalfRange, horizontalHalfRange), Random.Range(-verticalHalfRange, verticalHalfRange));
        direction = new Vector2(Random.Range(-horizontalHalfRange, horizontalHalfRange), Random.Range(-verticalHalfRange, verticalHalfRange)).normalized;
    }

    public void Update()
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
    List<Point> points;
    public TrianglePool trianglePool;
    public HashSet<Triangle> triangulation; 
    public float scalePlayground;
    public int triangulationAlg;
    Triangle superTriangle;

    private void Start()
    {
        halfHeight = Camera.main.orthographicSize;
        halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        points = new List<Point>();
        trianglePool = new TrianglePool();
        triangulation = new HashSet<Triangle>();
        GeneratePoints();
        Triangulate();

        // Unity sets a maximum delta time to avoid abnormal behaviour in games
        // this triangulation can go slow with high number of points, so we set it 
        // to virtually unlimited
        Time.maximumDeltaTime = 10.0f;
    }

    void Update()
    {
        // input checking
        if (Input.GetKeyDown(KeyCode.Space))
            GeneratePoints();
        if (Input.GetKey(KeyCode.Escape))        
            Application.Quit();        

        // update all the points
        for (int i = 0; i < points.Count; i++)
            points[i].Update();

        // TRIANGULATE
        switch (triangulationAlg)
        {
            case 0: Triangulate();     break;
            case 1: ContiguousTriangulate(); break;
        }
    }

    // generate new random points
    private void GeneratePoints()
    {           
        points = new List<Point>();
        //Random
        for (int i = 0; i < howManyPoints; i++)
        {            
            points.Add(new Point(halfWidth * scalePlayground, halfHeight * scalePlayground));
        }
    }

    /* loop through each point to add, brute force search through each triangle, if
	*  the point is contained in the triangle's circumcircle remove it. Create a polygon with
	*  each removed triangle's outer edge, and add the triangles made by linking each edge to
	*  the new point
	*/
    private void Triangulate()
    {
        triangulation.Clear();
        trianglePool.Clear();
        // first triangle, containing the whole playground
        superTriangle = trianglePool.Get();
        superTriangle.Populate(new Vector2(-halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(halfWidth * 2.5f * scalePlayground, -halfHeight * 2 * scalePlayground),
                                     new Vector2(0.0f, halfHeight * 3 * scalePlayground));
        triangulation.Add(superTriangle);

        List<Triangle> badTriangles = new List<Triangle>();
        List<Edge> polygon = new List<Edge>();

        for (int i = 0; i < points.Count; i++)
        { 
            Vector2 point = points[i].position;
            badTriangles.Clear();
            polygon.Clear();

            // check if the triangle contains point in its circumcircle
            foreach(Triangle triangle in triangulation)            
                if (triangle.isPointInsideCircumcircle(point))
                    badTriangles.Add(triangle);

            // create the outer polygon
            for (int outer = 0; outer < badTriangles.Count; outer++) 
            {
                for (int edge = 0; edge < 3; edge++)           
                {
                    bool isShared = false;
                    for (int inner = 0; inner < badTriangles.Count; inner++) 
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

            // remove bad triangles
            for (int j = 0; j < badTriangles.Count; j++)
            {
                trianglePool.Remove(badTriangles[j]);
                triangulation.Remove(badTriangles[j]);
            }
            
            // create new triangles
            for (int j = 0; j < polygon.Count; j++)
            {
                Triangle thisTriangle = trianglePool.Get();
                thisTriangle.Populate(polygon[j].A, polygon[j].B, point);
                triangulation.Add(thisTriangle);
            }
        }
    }

    /* Smarter implementation, based on the assumption that only contiguous triangles can contain
	*  the point in their circumcircle. Main differences are 1) as we find the first "bad" triangle, 
	*  we just check the contiguous ones 2) we need to keep the triangles linked together
	*/
    private void ContiguousTriangulate()
    {
        // recursive function to check contiguous if contiguous triangles contain the new point in their circumcircle
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

        // find if triangle1 shares one edge with triangle2, and link them
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

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 point = points[i].position;           

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

            foreach(Triangle badTriangle in badTriangles)
            {
                for (int edge = 0; edge < 3; edge++)
                {
                    if (!badTriangles.Contains(badTriangle.links[edge]))
                    {
                        polygon.Add(badTriangle.edges[edge]);
                        // save the link with outer triangles
                        if (badTriangle.links[edge] != null &&
                            !badTriangles.Contains(badTriangle.links[edge]))
                            outerTriangles.Add(badTriangle.links[edge]);
                    }
                }
            }

            foreach (Triangle badTriangle in badTriangles)
            {
                trianglePool.Remove(badTriangle);
                triangulation.Remove(badTriangle);
            }

            // insert new triangles into a "partial" triangulation, allowing for linking
            for (int j = 0; j < polygon.Count; j++)
            {
                Triangle thisTriangle = trianglePool.Get();
                thisTriangle.Populate(polygon[j].A, polygon[j].B, point);
                partialTriangulation.Add(thisTriangle);
            }

            foreach (Triangle partialTriangle in partialTriangulation)
            {
                // link with outer triangles
                foreach (Triangle outerTriangle in outerTriangles)
                    if (linkTriangles(partialTriangle, outerTriangle))
                    {
                        linkTriangles(outerTriangle, partialTriangle);
                        break;
                    }

                // link "partial" triangles with each other
                int maxTwoLinked = 0;
                foreach (Triangle otherPartialTriangle in partialTriangulation)
                {
                    if (linkTriangles(partialTriangle, otherPartialTriangle))
                    {
                        maxTwoLinked++;
                    }
                    if (maxTwoLinked == 2) break;
                }

                triangulation.Add(partialTriangle);
            }
        }
    }

    // called by UI
    public void OnChangeAmount(int value)
    {
        howManyPoints = value;
        GeneratePoints();
    }

    // called by UI
    public void OnChangeBackgroundColor(Color color)
    {
        Camera.main.backgroundColor = color;
    }
}
