using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlaceObject : MonoBehaviour
{
    public GameObject pinPrefab;
    public GameObject linePrefab;
    public GameObject floor;
    public GameObject placementIndicator;
    public ARRaycastManager raycastManager;
    private Pose placementPose;
    public Camera arCamera;

    private bool cycle;
    private bool isMakingFloor;
    float xRotation = 0f;
    float startRotation;
    public float floorHeight = 1f;

    private GameObject currentLine;
    private Vector3 currentLineFixedPose;
    private List<GameObject> placedPins = new List<GameObject>();
    private List<GameObject> placedLines = new List<GameObject>();

    public LayerMask pinMask;
    public GameObject mark;
    void Update()
    {
        if (isMakingFloor)
        {
            MeasuringHeight();
        }
        else
        {
            UpdateMidPointer();
            ShowLine();
        }
        Interaction();
    }

    void UpdateMidPointer()
    {
        var screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();
        raycastManager.Raycast(screenCenter, hits, TrackableType.Planes);
        
        if (hits.Count > 0)
        {
            placementPose = hits[0].pose;
            var cameraForward = Camera.main.transform.forward;
            var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            placementPose.rotation = Quaternion.LookRotation(cameraBearing);

            placementIndicator.SetActive(true);
            // �� ������ ������ ���
            if (IsCycle() && placedPins.Count > 2)
            {
                cycle = true;
                placementIndicator.transform.SetPositionAndRotation(placedPins[0].transform.position, placedPins[0].transform.rotation);

                placedPins.Add(Instantiate(pinPrefab, placedPins[0].transform.position, Quaternion.identity));
                MakeFloor();
            }
            else
            {
                cycle = false;
                placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
            }
        }
        else
        {
            placementIndicator.SetActive(false);
        }

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;
        // ���̰� � ������Ʈ�� �浹�ߴ��� �˻��մϴ�.
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, pinMask))
        {
            mark.SetActive(true);
        }
        else
        {
            mark.SetActive(false);
        }
    }

    bool IsCycle()
    {
        if (placedPins.Count == 0) return false;
        var bounds = placedPins[0].GetComponentInChildren<Renderer>().bounds;
        //Debug.Log("�浹�ߴ°�? " + bounds.Contains(placementPose.position));
        return bounds.Contains(placementPose.position);
    }

    void Interaction() {
        // Ȯ�� Ŀ�ǵ�
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            // �� ����
            if (!cycle) {
                FixPin();
            }
            // �ٴ� ���� ����
            else if (isMakingFloor)
            {

            }
        }
        // ���� ��� Ŀ�ǵ�
        else if (Input.touchCount == 2 && placedPins.Count > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            // �� ���� ���� ����
            if (cycle || isMakingFloor)
            {
                cycle = false;
                isMakingFloor = false;
                floor.SetActive(false);
            }
            // �� ����
            else
            {
                RemovePin();
            }
        }
    }

    void ShowLine()
    {
        if (currentLine == null)
            return;
        currentLine.SetActive(placementIndicator.activeSelf);
        if (placementIndicator.activeSelf)
        {
            Vector3 direction = (placementPose.position - currentLineFixedPose).normalized;

            // ������Ʈ�� ȸ���� ���� ���Ϳ� �°� ����
            currentLine.transform.rotation = Quaternion.LookRotation(direction);

            // �Ÿ��� ���� ������ ���� (���÷�, ���⼭�� Z�� �������� �����մϴ�)
            float distance = Vector3.Distance(currentLineFixedPose, placementPose.position);
            currentLine.transform.localScale = new Vector3(currentLine.transform.localScale.x, currentLine.transform.localScale.y, distance);

            // ������Ʈ�� ��ġ�� ���� (�������� ���콺 ��ġ�� �߰������� ����)
            currentLine.transform.position = currentLineFixedPose + direction * distance / 2;
        }
    }

    /**
     * �� ����
     */
    void FixPin()
    {
        
        if (placedPins.Count == 0)
        {
            GameObject currentPin = Instantiate(pinPrefab, placementPose.position, Quaternion.identity);
            currentPin.layer = LayerMask.NameToLayer("Pin");
        }
        else
            placedPins.Add(Instantiate(pinPrefab, placementPose.position, Quaternion.identity));
        if (currentLine != null)
            placedLines.Add(currentLine);

        currentLine = Instantiate(linePrefab, placementPose.position, placementPose.rotation);
        currentLineFixedPose = placedPins[^1].transform.position;
    }

    void RemovePin()
    {
        if (placedPins.Count == 0) return;

        Destroy(placedPins[^1]);
        placedPins.Remove(placedPins[^1]);

        if (placedLines.Count > 0)
        {
            Destroy(placedLines[^1]);
            placedLines.Remove(placedLines[^1]);
            currentLineFixedPose = placedPins[^1].transform.position;
        }
        else
        {
            Destroy(currentLine);
            currentLine = null;
        }
    }

    void MakeFloor()
    {
        float avgY = 0f;
        int numVertices = placedPins.Count - 1;
        for (int i = 0; i < numVertices; i++)
        {
            avgY += placedPins[i].transform.position.y;
        }
        avgY /= placedPins.Count;

        Vector3[] vertices = new Vector3[numVertices];
        for (int i = 0; i < numVertices; i++)
        {
            vertices[i] = new Vector3(placedPins[i].transform.position.x, avgY, placedPins[i].transform.position.z);
        }

        // �ٰ����� �޽� ����
        Mesh meshUp = new Mesh();
        Mesh meshDown = new Mesh();
        // �޽��� ������ �� �ﰢ�� ���� ����
        meshUp.vertices = vertices;
        meshDown.vertices = vertices;

        int[] trianglesUp = new int[(numVertices - 2) * 3];
        int[] trianglesDown = new int[(numVertices - 2) * 3];
        for (int i = 0, j = 0; i < trianglesDown.Length; i += 3, j++)
        {
            trianglesDown[i] = 0;
            trianglesDown[i + 1] = j + 1;
            trianglesDown[i + 2] = j + 2;
        }

        for (int i = 0, j = 0; i < trianglesUp.Length; i += 3, j++)
        {
            trianglesUp[i] = j + 2;  // ù ��° �������� �ε���
            trianglesUp[i + 1] = j + 1;  // �� ��° �������� �ε���
            trianglesUp[i + 2] = 0;  // �� ��° �������� �ε���
        }
        meshUp.triangles = trianglesUp;
        meshDown.triangles = trianglesDown;

        Vector3[] normalsUp = new Vector3[numVertices];
        Vector3[] normalsDown = new Vector3[numVertices];
        for (int i = 0; i < numVertices; i++)
        {
            normalsUp[i] = Vector3.up;
            normalsDown[i] = Vector3.down;
        }
        meshUp.normals = normalsUp;
        meshDown.normals = normalsDown;
        floor.transform.GetChild(0).GetComponent<MeshFilter>().mesh = meshUp;
        floor.transform.GetChild(0).GetComponent<MeshCollider>().sharedMesh = meshUp;
        floor.transform.GetChild(0).transform.position = new Vector3(0, -avgY, 0);

        floor.transform.GetChild(1).GetComponent<MeshFilter>().mesh = meshDown;
        floor.transform.GetChild(1).GetComponent<MeshCollider>().sharedMesh = meshDown;
        floor.transform.GetChild(1).transform.position = new Vector3(0, -avgY, 0);

        startRotation = xRotation;
        floor.SetActive(true);
        isMakingFloor = true;
    }

    void MeasuringHeight()
    {

    }
}
