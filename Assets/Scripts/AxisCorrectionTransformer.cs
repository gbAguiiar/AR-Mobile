using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

/// Preserves non-Y-axis rotation (e.g. a +90° X correction baked into a prefab)
/// when ARTransformer resets rotation to the plane pose on every grab.
///
/// Place this component on the same GameObject as ARTransformer and XRGrabInteractable.
/// It must appear AFTER ARTransformer in the Inspector component list so it runs second
/// and overwrites the (wrong) rotation ARTransformer produced.
[AddComponentMenu("XR/Transformers/Axis Correction Transformer")]
public class AxisCorrectionTransformer : XRBaseGrabTransformer
{
    Quaternion m_GrabRotation;
    float m_AccumulatedYDelta;
    IXRSelectInteractor m_Interactor;
    Vector3 m_LastInteractorEuler;

    public override void OnGrab(XRGrabInteractable grabInteractable)
    {
        base.OnGrab(grabInteractable);
        m_GrabRotation = grabInteractable.transform.rotation;
        m_AccumulatedYDelta = 0f;
    }

    public override void OnGrabCountChanged(XRGrabInteractable grabInteractable, Pose targetPose, Vector3 localScale)
    {
        base.OnGrabCountChanged(grabInteractable, targetPose, localScale);
        m_Interactor = grabInteractable.interactorsSelecting[0];
        m_LastInteractorEuler = m_Interactor.GetAttachTransform(grabInteractable).localEulerAngles;
        m_AccumulatedYDelta = 0f;
    }

    public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
    {
        switch (updatePhase)
        {
            case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
                if (m_Interactor != null)
                {
                    var currentEuler = m_Interactor.GetAttachTransform(grabInteractable).localEulerAngles;
                    var delta = currentEuler.y - m_LastInteractorEuler.y;
                    if (delta > 180f) delta -= 360f;
                    if (delta < -180f) delta += 360f;
                    m_AccumulatedYDelta += delta;
                    m_LastInteractorEuler = currentEuler;
                }
                goto case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender;

            case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
                // Re-compose: apply accumulated Y rotation around world-up on top of the
                // original grab rotation (which already contains the axis correction).
                targetPose.rotation = Quaternion.Euler(0f, m_AccumulatedYDelta, 0f) * m_GrabRotation;
                break;
        }
    }
}
