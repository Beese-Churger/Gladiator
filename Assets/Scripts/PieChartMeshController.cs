using UnityEngine;

public class PieChartMeshController : MonoBehaviour
{
    PieChartMesh mPieChart;
    float[] mData = { 1f, 1f, 1f };

    void Start()
    {
        mPieChart = gameObject.AddComponent<PieChartMesh>() as PieChartMesh;

        mPieChart.Init(mData, 100, 0, 100, null);
        mPieChart.Draw(mData);
        //MeshCollider col = gameObject.AddComponent<MeshCollider>();
        //col.convex = true;
    }

    void Update()
    {
        //if (Input.GetKeyDown("a"))
        //{
        //    mData = GenerateRandomValues(4);
        //    mPieChart.Draw(mData);
        //}
    }

    float[] GenerateRandomValues(int length)
    {
        float[] targets = new float[length];

        for (int i = 0; i < length; i++)
        {
            targets[i] = Random.Range(0f, 100f);
        }
        return targets;
    }
}
