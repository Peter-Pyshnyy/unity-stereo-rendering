using UnityEngine;
using System;
using Apt.Unity.Projection;

public class stereoCamera : MonoBehaviour
{
    [Range(0.0F, 1.0F)]
    public float distance = 0.0F;
    public Camera leftEye;
    public Camera rightEye;
    public ProjectionPlane projectionPlane;
    public bool offAxisProjection = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        leftEye.transform.localPosition = Vector3.left * distance / 2f;
        rightEye.transform.localPosition = Vector3.right * distance / 2f;
    }

    // Update is called once per frame
    void Update()
    {
        leftEye.transform.localPosition = Vector3.left * distance / 2f;
        rightEye.transform.localPosition = Vector3.right * distance / 2f;
        ApplyOffAxisProjection(leftEye);
        ApplyOffAxisProjection(rightEye);
    }

    private void ApplyOffAxisProjection(Camera camera)
    {
        if (!offAxisProjection)
        {
            camera.ResetProjectionMatrix();
            camera.ResetWorldToCameraMatrix();
            return;
        }

        if (!projectionPlane)
            throw new Exception("No projection plane set!");

        camera.projectionMatrix = GetProjectionMatrix(projectionPlane, camera);

        // Translation to eye position
        var planeWorldMatrix = projectionPlane.M;
        var relativePlaneRotationMatrix = Matrix4x4.Rotate(
           Quaternion.Inverse(transform.rotation) * projectionPlane.transform.rotation);
        var cameraTranslationMatrix = Matrix4x4.Translate(-camera.transform.position);
        camera.worldToCameraMatrix = planeWorldMatrix * relativePlaneRotationMatrix * cameraTranslationMatrix;
    }

    private static Matrix4x4 GetProjectionMatrix(ProjectionPlane projectionPlane, Camera camera)
    {
        Vector3 pa = projectionPlane.BottomLeft;
        Vector3 pb = projectionPlane.BottomRight;
        Vector3 pc = projectionPlane.TopLeft;
        Vector3 pd = projectionPlane.TopRight;

        Vector3 vr = projectionPlane.DirRight;
        Vector3 vu = projectionPlane.DirUp;
        Vector3 vn = projectionPlane.DirNormal;


        // Your implementation starts here:

        Vector3 pe = camera.transform.position;
        Vector3 va = pa - pe;
        Vector3 vb = pb - pe;
        Vector3 vc = pc - pe;
        float d = -Vector3.Dot(vn, va); // distance to the far plane?
        float n = camera.nearClipPlane;
        float f = camera.farClipPlane;
        float l = Vector3.Dot(vr, va) * n / d;
        float r = Vector3.Dot(vr, vb) * n / d;
        float b = Vector3.Dot(vu, va) * n / d;
        float t = Vector3.Dot(vu, vc) * n / d;


        return Matrix4x4.Frustum(l, r, b, t, n, f);
    }
}
