using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlaceObject : MonoBehaviour
{
    public GameObject pinPrefab;
    public GameObject linePrefab;
    public GameObject placementIndicator;
    public ARRaycastManager raycastManager;
    private Pose placementPose;
    public Camera arCamera;
    public List<ARRaycastHit> touchHits = new List<ARRaycastHit>();

    private GameObject currentLine;
    private Vector3 currentLineFixedPose;
    private List<GameObject> placedPins = new List<GameObject>();
    private List<GameObject> placedLines = new List<GameObject>();

    void Update()
    {
        UpdateMidPointer();
        SetPin();
        ShowLine();
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
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }

    void SetPin()
    {
        // 핀 세팅
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended)
            FixPin();
        // 실행 취소 커맨드
        else if (Input.touchCount == 2 && placedPins.Count > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            RemovePin();
    }

    void ShowLine()
    {
        if (currentLine == null)
            return;

        Vector3 direction = (placementPose.position - currentLineFixedPose).normalized;

        // 오브젝트의 회전을 방향 벡터에 맞게 조정
        currentLine.transform.rotation = Quaternion.LookRotation(direction);

        // 거리에 따른 스케일 조정 (예시로, 여기서는 Z축 스케일을 조정합니다)
        float distance = Vector3.Distance(currentLineFixedPose, placementPose.position);
        currentLine.transform.localScale = new Vector3(currentLine.transform.localScale.x, currentLine.transform.localScale.y, distance);

        // 오브젝트의 위치를 조정 (고정점과 마우스 위치의 중간점으로 설정)
        currentLine.transform.position = currentLineFixedPose + direction * distance / 2;
    }

    /**
     * 핀 고정
     */
    void FixPin()
    {
        placedPins.Add(Instantiate(pinPrefab, placementPose.position, Quaternion.identity));
        if (currentLine != null)
            placedLines.Add(currentLine);

        currentLine = Instantiate(linePrefab, placementPose.position, placementPose.rotation);
        currentLineFixedPose = placedPins[^1].transform.position;
    }

    void RemovePin()
    {
        Destroy(placedPins[^1]);
        placedPins.Remove(placedPins[^1]);
        if (placedLines.Count > 0)
        {
            Destroy(placedLines[^1]);
            placedLines.Remove(placedLines[^1]);
            if (placedPins.Count > 0) 
                currentLineFixedPose = placedPins[^1].transform.position;
        }
    }
}
