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
    public List<int> links; // ^^^
    public float circumRadius;
    public Vector2 circumCenter;    
    public int poolIndex;

    public Triangle(int index)
    {
        pointA = new Vector2(0f, 0f);
        pointB = new Vector2(0f, 0f);
        pointC = new Vector2(0f, 0f);
        edges  = new List<Edge>();
        links  = new List<int>();

        edges.Add(new Edge(pointA, pointB));
        links.Add(-1);
        edges.Add(new Edge(pointB, pointC));
        links.Add(-1);
        edges.Add(new Edge(pointC, pointA));
        links.Add(-1);

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
        links[0] = -1;
        links[1] = -1;
        links[2] = -1;
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

public class TriangulationManager : MonoBehaviour
{
    
    public int howManyPoints;
    float halfHeight, halfWidth;
    List<GameObject> points;
    public TrianglePool trianglePool;
    public List<Triangle> triangulation;
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
        Dictionary<Edge, int> badEdges = new Dictionary<Edge, int>();

        for (int i = 0; i < howManyPoints; i++)
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
                triangulation.Remove(badTriangles[j]);
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
    
    /*private void ContiguousTriangulate()
    {
        void checkEdges(ref List<Triangle> _badTriangles, Vector2 point, Triangle triangle)
        {
            for (int edge=0; edge < 3; edge++)
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
        List<Triangle> partialTriangulation = new List<Triangle>();

        for (int i = 0; i < howManyPoints; i++)
        {
            Profiler.BeginSample("Finding Bad Triangles");
            Vector2 point = pointContainerGO.transform.GetChild(i).transform.position;

            badTriangles.Clear();
            for (int j = triangulation.Count - 1; j >= 0; j--)
                if (triangulation[j].isPointInsideCircumcircle(point))
                {
                    badTriangles.Add(triangulation[j]);
                    checkEdges(ref badTriangles, point, triangulation[j]);
                    break;
                }

            Profiler.EndSample();

            Profiler.BeginSample("Allocation");
            polygon.Clear();
            outerTriangles.Clear();
            Profiler.EndSample();

            Profiler.BeginSample("Finding Polygon");

            //for (int outer = 0; outer < badTriangles.Count; outer++) // triangles
            //{
            //    for (int edge = 0; edge < 3; edge++)
            //    {
            //        bool isShared = false;
            //        for (int inner = 0; inner < badTriangles.Count; inner++) // triangles
            //        {
            //            if (inner != outer && !isShared)
            //            {
            //                for (int badEdge = 0; badEdge < 3; badEdge++)
            //                    if (badTriangles[outer].edges[edge].Compare(badTriangles[inner].edges[badEdge]))
            //                        isShared = true;
            //            }
            //        }
            //        if (!isShared)
            //        {
            //            polygon.Add(badTriangles[outer].edges[edge]);
            //            if (point != badTriangles[outer].edges[edge].A && point != badTriangles[outer].edges[edge].B
            //                && badTriangles[outer].links[edge] != null &&
            //                !outerTriangles.Contains(badTriangles[outer].links[edge]))
            //                outerTriangles.Add(badTriangles[outer].links[edge]);
            //        }
            //    }
            //}

            for (int badTriangle = 0; badTriangle < badTriangles.Count; badTriangle++)
            {
                for (int edge = 0; edge < 3; edge++)
                {
                    if (!badTriangles.Contains(badTriangles[badTriangle].links[edge]))
                    {
                        polygon.Add(badTriangles[badTriangle].edges[edge]);
                        if (badTriangles[badTriangle].links[edge] != null)
                        {
                            outerTriangles.Add(badTriangles[badTriangle].links[edge]);
                        }
                    }
                }    
            }            

            Profiler.EndSample();

            for (int j = 0; j < badTriangles.Count; j++)
            {
            Profiler.BeginSample("Removing bad Triangles");
                triangulation.Remove(badTriangles[j]);
            Profiler.EndSample();
                trianglePool.Remove(badTriangles[j]);
            }

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
        



    }*/

    private void ContiguousTriangulate()
    {
        void checkEdges(List<int> _badTriangles, Vector2 point, int triangle)
        {
            for (int edge=0; edge < 3; edge++)
            {
                if (triangulation[triangle].links[edge] != -1 &&
                    triangulation[triangulation[triangle].links[edge]].isPointInsideCircumcircle(point) &&
                    !_badTriangles.Contains(triangulation[triangle].links[edge]))
                {
                    _badTriangles.Add(triangulation[triangle].links[edge]);
                    checkEdges(_badTriangles, point, triangulation[triangle].links[edge]);                    
                }
            }
        }

        void linkTriangles(Triangle triangle1, Triangle triangle2, int triangle1Pos, int triangle2Pos)
        {
            if (triangle1 != triangle2 && triangle1 != null && triangle2 != null)
            {
                for (int outer = 0; outer < 3; outer++)
                    for (int inner = 0; inner < 3; inner++)
                    {
                        if (triangle1.edges[outer].Compare(triangle2.edges[inner]))
                        {
                            triangle1.links[outer] = triangle2Pos;
                            triangle2.links[inner] = triangle1Pos;
                            return;
                        }
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
        List<int> badTriangles = new List<int>();
        List<Triangle> outerTriangles = new List<Triangle>();
        List<Triangle> partialTriangulation = new List<Triangle>();

        for (int i = 0; i < howManyPoints; i++)
        {
            Profiler.BeginSample("Finding Bad Triangles");
            Vector2 point = pointContainerGO.transform.GetChild(i).transform.position;

            polygon.Clear();
            outerTriangles.Clear();
            partialTriangulation.Clear();
            badTriangles.Clear();
            for (int j = triangulation.Count - 1; j >= 0; j--)
                if (triangulation[j].isPointInsideCircumcircle(point))
                {
                    badTriangles.Add(j);
                    checkEdges(badTriangles, point, j);
                    break;
                }

            Profiler.EndSample();

            Profiler.BeginSample("Finding Polygon");

            //for (int outer = 0; outer < badTriangles.Count; outer++) // triangles
            //{
            //    for (int edge = 0; edge < 3; edge++)
            //    {
            //        bool isShared = false;
            //        for (int inner = 0; inner < badTriangles.Count; inner++) // triangles
            //        {
            //            if (inner != outer && !isShared)
            //            {
            //                for (int badEdge = 0; badEdge < 3; badEdge++)
            //                    if (badTriangles[outer].edges[edge].Compare(badTriangles[inner].edges[badEdge]))
            //                        isShared = true;
            //            }
            //        }
            //        if (!isShared)
            //        {
            //            polygon.Add(badTriangles[outer].edges[edge]);
            //            if (point != badTriangles[outer].edges[edge].A && point != badTriangles[outer].edges[edge].B
            //                && badTriangles[outer].links[edge] != null &&
            //                !outerTriangles.Contains(badTriangles[outer].links[edge]))
            //                outerTriangles.Add(badTriangles[outer].links[edge]);
            //        }
            //    }
            //}

            badTriangles.Sort();

            for (int badTriangle = 0; badTriangle < badTriangles.Count; badTriangle++)
            {
                for (int edge = 0; edge < 3; edge++)
                {
                    if (!badTriangles.Contains(triangulation[badTriangles[badTriangle]].links[edge]))
                    {
                        polygon.Add(triangulation[badTriangles[badTriangle]].edges[edge]);
                        if (triangulation[badTriangles[badTriangle]].links[edge] != -1 && 
                            !outerTriangles.Contains(triangulation[triangulation[badTriangles[badTriangle]].links[edge]]))
                            outerTriangles.Add(triangulation[triangulation[badTriangles[badTriangle]].links[edge]]);                        
                    }
                }
            }

            Profiler.EndSample();

            for (int j = badTriangles.Count - 1; j >= 0; j--)
            {
                trianglePool.Remove(triangulation[badTriangles[j]]);
                Profiler.BeginSample("Removing bad Triangles");
                triangulation.RemoveAt(badTriangles[j]);
                Profiler.EndSample();
            }

            Profiler.EndSample();
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
                    linkTriangles(partialTriangulation[j], outerTriangles[k]);
                
                Profiler.EndSample();

                Profiler.BeginSample("Second");
                for (int k = 0; k < partialTriangulation.Count; k++)
                    linkTriangles(partialTriangulation[j], partialTriangulation[k]);
                Profiler.EndSample();

                triangulation.Add(partialTriangulation[j]);
            }

            //Profiler.EndSample();
        }
        



    }
}
