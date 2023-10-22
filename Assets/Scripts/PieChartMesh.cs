
using UnityEngine;

public class PieChartMesh : MonoBehaviour
{

    float[] mData;

    int mSlices;
    float mRotationAngle;
    float mRadius;
    Material[] mMaterials;

    Vector3[] mVertices;
    Vector3[] mNormals;
    Vector3 mNormal = new Vector3(0f, 0f, -1f);
    Vector2[] mUvs;
    int[] mTriangles;

    MeshRenderer mMeshRenderer;
 

    public void Init(float[] data, int slices, float rotatioAngle, float radius, Material[] materials)
    {
        mData = data;
        mSlices = slices;
        mRotationAngle = rotatioAngle;
        mRadius = radius;

        // Get Mesh Renderer
        mMeshRenderer = gameObject.GetComponent("MeshRenderer") as MeshRenderer;
        if (mMeshRenderer == null)
        {
            gameObject.AddComponent<MeshRenderer>();
            mMeshRenderer = gameObject.GetComponent("MeshRenderer") as MeshRenderer;
        }
        
        mMaterials = materials;


        Init(data);
    }

    public void Init(float[] data)
    {
        mSlices = 100;
        mRotationAngle = 90f;
        mRadius = 0.3f;

        mData = data;
    }

    public void Draw(float[] data)
    {
        mData = data;
        Draw();
    }

    public void Draw()
    {

        //Check data validity for pie chart...
        if (mData == null)
        {
            print("PieChart: Data null");
            return;
        }

        for (int i = 0; i < mData.Length; i++)
        {
            if (mData[i] < 0)
            {
                print("PieChart: Data < 0");
                return;
            }
        }

        // Calculate sum of data values
        float sumOfData = 0;
        foreach (float value in mData)
        {
            sumOfData += value;
        }
        if (sumOfData <= 0)
        {
            print("PieChart: Data sum <= 0");
            return;
        }
        // Determine how many triangles in slice
        int[] slice = new int[mData.Length];
        int numOfTris = 0;
        int numOfSlices = 0;
        int countedSlices = 0;

        // Caluclate slice size 
        for (int i = 0; i < mData.Length; i++)
        {
            numOfTris = (int)((mData[i] / sumOfData) * mSlices);
            slice[numOfSlices++] = numOfTris;
            countedSlices += numOfTris;
        }
        // Check that all slices are counted.. if not -> add/sub to/from biggest slice..
        int idxOfLargestSlice = 0;
        int largestSliceCount = 0;
        for (int i = 0; i < mData.Length; i++)
        {
            if (largestSliceCount < slice[i])
            {
                idxOfLargestSlice = i;
                largestSliceCount = slice[i];
            }
        }

        // Check validity for pie chart
        if (countedSlices == 0)
        {
            print("PieChart: Slices == 0");
            return;
        }

        // Adjust largest dataset to get proper slice 
        slice[idxOfLargestSlice] += mSlices - countedSlices;

        // Check validity for pie chart data
        if (slice[idxOfLargestSlice] <= 0)
        {
            print("PieChart: Largest pie <= 0");
            return;
        }

        // Init vertices and triangles arrays
        mVertices = new Vector3[mSlices * 3];
        mNormals = new Vector3[mSlices * 3];
        mUvs = new Vector2[mSlices * 3];
        mTriangles = new int[mSlices * 3];

        //gameObject.AddComponent("MeshFilter");
        //gameObject.AddComponent("MeshRenderer");

        Mesh mesh = ((MeshFilter)GetComponent("MeshFilter")).mesh;

        mesh.Clear();

        mesh.name = "Pie Chart Mesh";

        // Roration offset (to get star point to "12 o'clock")
        float rotOffset = mRotationAngle / 360f * 2f * Mathf.PI;

        // Calc the points in circle
        float angle;
        float[] x = new float[mSlices];
        float[] y = new float[mSlices];

        for (int i = 0; i < mSlices; i++)
        {
            angle = i * 2f * Mathf.PI / mSlices;
            x[i] = (Mathf.Cos(angle + rotOffset) * mRadius);
            y[i] = (Mathf.Sin(angle + rotOffset) * mRadius);
        }

        // Generate mesh with slices (vertices and triangles)
        for (int i = 0; i < mSlices; i++)
        {
            mVertices[i * 3 + 0] = new Vector3(0f, 0f, 0f);
            mVertices[i * 3 + 1] = new Vector3(x[i], y[i], 0f);
            // This will ensure that last vertex = first vertex..
            mVertices[i * 3 + 2] = new Vector3(x[(i + 1) % mSlices], y[(i + 1) % mSlices], 0f);

            mNormals[i * 3 + 0] = mNormal;
            mNormals[i * 3 + 1] = mNormal;
            mNormals[i * 3 + 2] = mNormal;

            mUvs[i * 3 + 0] = new Vector2(0f, 0f);
            mUvs[i * 3 + 1] = new Vector2(x[i], y[i]);
            // This will ensure that last uv = first uv..
            mUvs[i * 3 + 2] = new Vector2(x[(i + 1) % mSlices], y[(i + 1) % mSlices]);

            mTriangles[i * 3 + 0] = i * 3 + 0;
            mTriangles[i * 3 + 1] = i * 3 + 1;
            mTriangles[i * 3 + 2] = i * 3 + 2;
        }


        // Assign verts, norms, uvs and tris to mesh and calc normals
        mesh.vertices = mVertices;
        mesh.normals = mNormals;
        mesh.uv = mUvs;
        //mesh.triangles = triangles;

        mesh.subMeshCount = mData.Length;

        int[][] subTris = new int[mData.Length][];

        countedSlices = 0;

        // Set sub meshes
        for (int i = 0; i < mData.Length; i++)
        {
            // Every triangle has three veritces..
            subTris[i] = new int[slice[i] * 3];

            // Add tris to subTris
            for (int j = 0; j < slice[i]; j++)
            {
                subTris[i][j * 3 + 0] = mTriangles[countedSlices * 3 + 0];
                subTris[i][j * 3 + 1] = mTriangles[countedSlices * 3 + 1];
                subTris[i][j * 3 + 2] = mTriangles[countedSlices * 3 + 2];

                countedSlices++;
            }
            mesh.SetTriangles(subTris[i], i);
        }
    }

    // Properties
    public float[] Data { get { return mData; } set { mData = value; } }

    public int Slices { get { return mSlices; } set { mSlices = value; } }

    public float RotationAngle { get { return mRotationAngle; } set { mRotationAngle = value; } }

    public float Radius { get { return mRadius; } set { mRadius = value; } }

    public Material[] Materials { get { return mMaterials; } set { mMaterials = value; } }

}
