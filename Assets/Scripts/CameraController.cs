﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.PostProcessing;

public class CameraController : MonoBehaviour
{

    public GameObject PlayerObject;
    public PostProcessingBehaviour PostProcessingObject;
    public const float StartingXOffset = -4;
    public const float StartingYOffset = 4;
    public const float StartingZOffset = -4;
    public const float StartingXRotation = 30;
    public const float StartingYRotation = 45;
    public const float StartingZRotation = 0;

    //used for translation
    private Vector3 _offset;

    //used for rotation
    private bool _isKeyboardRotating;
    private const float KeyboardSpeed = 380f;
    private const float MouseXSpeed = 3f;
    private const float MouseYSpeed = 1f;
    private float _currentYDisplacement;
    private const float MaxYDisplacement = 50f;
    private PlayerController _playerController;
    private ArrayList _fadeBlocks = new ArrayList();
    private bool _idle = false;
    private const float SlowRotateSpeed = 0.3f;

    //used for zoom
    private const float MaxZoom = 6;
    private const float MinZoom = -6;
    private const float zoomSpeed = 3;
    private float currentZoom = 0;
    
    //used for autozoom
    private const float autoZoomSpeed = 0.04f;
    private float currentAutoZoomValue = 0;
    private float _zoomLeeway = 3;

    private void Awake()
    {
        _offset = new Vector3(StartingXOffset, StartingYOffset, StartingZOffset);
        transform.eulerAngles = new Vector3(StartingXRotation, StartingYRotation, StartingZRotation);
        _playerController = PlayerObject.GetComponent<PlayerController>();
        PostProcessingObject = GetComponent<PostProcessingBehaviour>();
    }

    private void Start()
    {
        _playerController.UpdateCamera();
    }

    private void Update()
    {
        Vector3 newPosition = PlayerObject.transform.position + _offset;
        transform.position = newPosition;

        if (!_idle)
        {
            RotateMouseCamera();

            if (Input.GetAxis("CameraZoom") > 0 || Input.GetAxis("CameraZoom") < 0)
            {
                ZoomCamera(Input.GetAxis("CameraZoom"));
            }
            else
            {
                AutoZoom();
            }
        }
        else
        {
            RotateSlowly();
        }

        UpdateFadingBlocks();

        UpdatePostProcessingDof();
    }

    private void UpdatePostProcessingDof()
    {
        PostProcessingObject.UpdateDof(_offset.magnitude);
    }

    private Vector2 CalculateScreenSizeInWorldCoords() {
        Camera cam = Camera.main;
        Vector3 p1 = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 p2 = cam.ViewportToWorldPoint(new Vector3(1, 0, cam.nearClipPlane));
        Vector3 p3 = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        float width = (p2 - p1).magnitude;
        float height = (p3 - p2).magnitude;

        Vector2 dimensions = new Vector2(width, height);

        return dimensions;
    }

    private bool IsPlayerObstructed(float distance)
    {
        Camera cameraObject = Camera.main;
        Vector2 dimensions = CalculateScreenSizeInWorldCoords();
        Vector3 centrePositionDifference = transform.position - PlayerObject.transform.position;

        if (Physics.Raycast(PlayerObject.transform.position, (centrePositionDifference + Vector3.left * (dimensions.x / 2) + Vector3.up * (dimensions.y / 2)), distance))
            return true;
        if (Physics.Raycast(PlayerObject.transform.position, (centrePositionDifference + Vector3.left * (dimensions.x / 2) - Vector3.up * (dimensions.y / 2)), distance))
            return true;
        if (Physics.Raycast(PlayerObject.transform.position, (centrePositionDifference - Vector3.left * (dimensions.x / 2) - Vector3.up * (dimensions.y / 2)), distance))
            return true;
        if (Physics.Raycast(PlayerObject.transform.position, (centrePositionDifference - Vector3.left * (dimensions.x / 2) + Vector3.up * (dimensions.y / 2)), distance))
            return true;

        return false;
    }

    private void AutoZoom()
    {
        if (IsPlayerObstructed(_offset.magnitude))
        {
            if (ZoomCamera(-autoZoomSpeed))
            {
                currentAutoZoomValue -= autoZoomSpeed;
            }
        }
        else if (currentAutoZoomValue < 0 &&
            !IsPlayerObstructed(_offset.magnitude + autoZoomSpeed * _zoomLeeway))
        {
            if (ZoomCamera(autoZoomSpeed))
            {
                currentAutoZoomValue += autoZoomSpeed;
            }
            else
            {
                currentAutoZoomValue = 0;
            }
        }
    }

    //returns true if zoom happened, false if not
    private bool ZoomCamera(float zoomAmount)
    {
        float zoomDifference = zoomSpeed * zoomAmount;
        if (!(currentZoom + zoomDifference > MaxZoom) && !(currentZoom + zoomDifference < MinZoom))
        {
            _offset = transform.position - PlayerObject.transform.position + Vector3.Normalize(transform.position - PlayerObject.transform.position) * zoomDifference;
            currentZoom += zoomDifference;
            return true;
        }
        else
        {
            return false;
        }
    }

    private void UpdateFadingBlocks()
    {
        Vector3 cameraPosition = transform.position;
        Vector3 cameraToPlayerPosition = cameraPosition - PlayerObject.transform.position;
        cameraToPlayerPosition.y = 0;
        foreach (FadeoutPlugin fadeoutPlugin in _fadeBlocks)
        {
            if (fadeoutPlugin.GetBlock() == null) continue;
            Vector3 cameraToBlockPosition =
                cameraPosition - fadeoutPlugin.GetBlock().transform.position;
            cameraToBlockPosition.y = 0;
            fadeoutPlugin.SetIsFading(cameraToBlockPosition.magnitude < cameraToPlayerPosition.magnitude);
        }
    }

    public void AddFadeoutTarget(FadeoutPlugin fadeoutPlugin)
    {
        _fadeBlocks.Add(fadeoutPlugin);
    }

    public void RemoveFadeoutTarget(FadeoutPlugin fadeoutPlugin)
    {
        _fadeBlocks.Remove(fadeoutPlugin);
    }

    private void RotateMouseCamera()
    {
        float rotateXAmount = (Input.GetAxisRaw("Mouse X")) * MouseXSpeed;
        float rotateYAmount = (Input.GetAxisRaw("Mouse Y")) * MouseYSpeed;
        _currentYDisplacement += rotateYAmount;
        if (_currentYDisplacement > MaxYDisplacement)
        {
            rotateYAmount -= _currentYDisplacement - MaxYDisplacement;
            _currentYDisplacement = MaxYDisplacement;
        }
        else if (_currentYDisplacement < -MaxYDisplacement)
        {
            rotateYAmount -= _currentYDisplacement + MaxYDisplacement;
            _currentYDisplacement = -MaxYDisplacement;
        }
        transform.RotateAround(PlayerObject.transform.position, Vector3.up, rotateXAmount);
        Vector3 normalVector = GetNormalVector();
        transform.RotateAround(PlayerObject.transform.position, normalVector, rotateYAmount);
        
        _playerController.UpdateCamera();
        _offset = transform.position - PlayerObject.transform.position;
    }

    private void RotateSlowly()
    {
        float rotateXAmount = SlowRotateSpeed;
        transform.RotateAround(PlayerObject.transform.position, Vector3.up, rotateXAmount);
        Vector3 normalVector = GetNormalVector();

        _playerController.UpdateCamera();
        _offset = transform.position - PlayerObject.transform.position;
    }

    private Vector3 GetNormalVector()
    {
        Vector3 playerFacing = transform.position - PlayerObject.transform.position;
        Vector3 planeCompletion = playerFacing - Vector3.up;
        return Vector3.Cross(playerFacing, planeCompletion);
    }

    public void SetIdle(bool idle)
    {
        _idle = idle;
    }
}
