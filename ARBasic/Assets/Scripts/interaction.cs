using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class interaction : MonoBehaviour
{
    // Start is called before the first frame update
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    public Camera mainCamera;
    public GameObject pin;
    public GameObject wall;
    public GameObject floor;
    public GameObject pins;
    public GameObject walls;
    public LayerMask floorMask;
    public LayerMask pinMask;
    public Material firstPinMaterial;


    float xRotation = 0f;
    float startRotation;
    public float floorHeight = 1f;
    private GameObject currentWall;
    private Vector3 currentWallFixedPos;
    private List<GameObject> fixedPins = new List<GameObject>();
    private List<GameObject> fixedWalls = new List<GameObject>();
    private bool isLastPin = false;
    public bool isMakingFloor = false;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // ���콺 Ŀ���� ��޴ϴ�.
        pin.SetActive(false);
        wall.SetActive(false);
    }

    void Update()
    {
        RotateCamera();
        if (isMakingFloor)
        {
            MeasuringHeight();
        }
        else if(!floor.activeSelf)
        {
            ShowPin();
            FixPin();
            ShowWall();
            RemovePin();
        }
        
    }

    void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // ���� ȸ�� ������ -90������ 90���� �����մϴ�.

        mainCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    void ShowPin()
    {
        int x = Screen.width / 2;
        int y = Screen.height / 2;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y));
        RaycastHit hit;
        // ���̰� � ������Ʈ�� �浹�ߴ��� �˻��մϴ�.
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, (floorMask | pinMask)))
        {
            pin.SetActive(true);
            if (hit.transform.CompareTag("Pin"))
            {
                pin.transform.position = hit.transform.position;
                isLastPin = true;
            }
            else
            {
                pin.transform.position = hit.point;
                isLastPin = false;
            }

        }
        else
        {
            pin.SetActive(false);
        }

    }


    void ShowPin2()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // ���̰� � ������Ʈ�� �浹�ߴ��� �˻��մϴ�.
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, floorMask))
            {
                pin.SetActive(true);
                pin.transform.position = hit.point;
            }
            else
            {
                pin.SetActive(false);
            }
        }
    }


    void ShowWall()
    {
        if (currentWall == null)
            return;
        currentWall.SetActive(pin.activeSelf);
        if (currentWall.activeSelf)
        {
            Vector3 direction = (pin.transform.position - currentWallFixedPos).normalized;

            // ������Ʈ�� ȸ���� ���� ���Ϳ� �°� ����
            currentWall.transform.rotation = Quaternion.LookRotation(direction);

            // �Ÿ��� ���� ������ ���� (���÷�, ���⼭�� Z�� �������� �����մϴ�)
            float distance = Vector3.Distance(currentWallFixedPos, pin.transform.position);
            currentWall.transform.localScale = new Vector3(currentWall.transform.localScale.x, currentWall.transform.localScale.y, distance);

            // ������Ʈ�� ��ġ�� ���� (�������� ���콺 ��ġ�� �߰������� ����)
            currentWall.transform.position = currentWallFixedPos + direction * distance / 2;
        }
    }

    void FixPin()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (pin.activeSelf)
            {
                GameObject currentFixedPin = Instantiate(pin, pin.transform.position, pin.transform.rotation, pins.transform);
                if (fixedPins.Count == 0)
                {
                    currentFixedPin.GetComponent<MeshRenderer>().material = firstPinMaterial;
                    Debug.Log(floorMask.value);
                    currentFixedPin.layer = LayerMask.NameToLayer("Pin");
                }
                fixedPins.Add(currentFixedPin);
                if (currentWall != null)
                    fixedWalls.Add(currentWall);

                if (isLastPin)
                {
                    currentWall = null;                    
                    MakeFloor();
                }
                else
                {
                    currentWall = Instantiate(wall, pin.transform.position, pin.transform.rotation, walls.transform);
                    currentWallFixedPos = fixedPins[fixedPins.Count - 1].transform.position;
                }

            }
        }
    }

    void RemovePin()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (fixedPins.Count > 0)
            {
                GameObject lastPin = fixedPins[fixedPins.Count - 1];
                fixedPins.RemoveAt(fixedPins.Count - 1);
                if (fixedWalls.Count > 0)
                {
                    GameObject lastWall = fixedWalls[fixedWalls.Count - 1];
                    fixedWalls.RemoveAt(fixedWalls.Count - 1);
                    Destroy(lastWall);
                    currentWallFixedPos = fixedPins[fixedWalls.Count - 1].transform.position;
                }
                else
                {
                    Destroy(currentWall);
                    currentWall = null;
                }
                Destroy(lastPin);
            }
        }
    }

    void MakeFloor()
    {
        //float avgX = 0f;
        float avgY = 0f;
        //float avgZ = 0f;
        int numVertices = fixedPins.Count - 1;
        for (int i = 0; i < numVertices; i++)
        {
            //avgX += fixedPins[i].transform.position.x;
            avgY += fixedPins[i].transform.position.y;
            //avgZ += fixedPins[i].transform.position.z;
        }
        //avgX /= fixedPins.Count;
        avgY /= fixedPins.Count;
        //avgZ /= fixedPins.Count;

        
        Vector3[] vertices = new Vector3[numVertices];
        for(int i = 0; i < numVertices; i++)
        {
            vertices[i] = new Vector3(fixedPins[i].transform.position.x, avgY, fixedPins[i].transform.position.z);
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

        Vector3[] normalsUp  = new Vector3[numVertices];
        Vector3[] normalsDown = new Vector3[numVertices];
        for (int i = 0; i < numVertices; i++)
        {
            normalsUp[i] = Vector3.up;
            normalsDown[i] = Vector3.down;
        }
        meshUp.normals = normalsUp;
        meshDown.normals = normalsDown;

/*        GameObject polygonUp = new GameObject("PolygonUp");
        GameObject polygonDown = new GameObject("PolygonDown");
        // �޽� ����

        // �޽� ���� �߰� �� �޽� ����
        MeshFilter meshFilterUp = polygonUp.AddComponent<MeshFilter>();
        meshFilterUp.mesh = meshUp;
        MeshCollider meshColliderUp = polygonUp.AddComponent<MeshCollider>();
        meshColliderUp.sharedMesh = meshUp;
        // �޽� ������ �߰�
        polygonUp.AddComponent<MeshRenderer>();
        polygonUp.transform.position = Vector3.zero;

        MeshFilter meshFilterDown = polygonDown.AddComponent<MeshFilter>();
        meshFilterDown.mesh = meshDown;
        MeshCollider meshColliderDown = polygonDown.AddComponent<MeshCollider>();
        meshColliderDown.sharedMesh = meshDown;
        // �޽� ������ �߰�
        polygonDown.AddComponent<MeshRenderer>();
        polygonDown.transform.position = Vector3.zero;*/

        floor.transform.GetChild(0).GetComponent<MeshFilter>().mesh = meshUp;
        floor.transform.GetChild(0).GetComponent<MeshCollider>().sharedMesh = meshUp;
        floor.transform.GetChild(0).transform.position = new Vector3(0, -avgY, 0);

        floor.transform.GetChild(1).GetComponent<MeshFilter>().mesh = meshDown;
        floor.transform.GetChild(1).GetComponent<MeshCollider>().sharedMesh = meshDown;
        floor.transform.GetChild(1).transform.position = new Vector3(0, -avgY, 0);

        //pins.SetActive(false);
        walls.SetActive(false);
        pin.SetActive(false);
        //pins.transform.localScale = new Vector3(1f, 5f, 1f);
        startRotation = xRotation;
        floor.SetActive(true);
        isMakingFloor = true;


    }



    void MeasuringHeight()
    {

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // ���̰� � ������Ʈ�� �浹�ߴ��� �˻��մϴ�.
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, pinMask))
        {
            if (hit.transform.CompareTag("Pin"))
            {
                floor.transform.position = new Vector3(0, hit.point.y, 0);
            }
            Debug.Log(hit.point.y);
        }


        if (Input.GetMouseButtonDown(0))
        {
            isMakingFloor = false;            
            walls.SetActive(true);
        }
    }
       

    void CheckConfirm()
    {
        //Physics.Raycast(ray, out hit, Mathf.Infinity, pinMask);
        if (Input.GetMouseButton(0))
        {
            
        }
    }
}
