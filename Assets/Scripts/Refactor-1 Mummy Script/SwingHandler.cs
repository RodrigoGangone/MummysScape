using UnityEngine;

public class SwingHandler : MonoBehaviour
{
    public SpringJoint SpringJoint { get; private set; }

    [SerializeField, Range(0f, 5f)] private float minDistance;
    [SerializeField, Range(0f, 5f)] private float maxDistance;
    [SerializeField, Range(0f, 200f)] private float spring;
    [SerializeField, Range(0f, 30f)] private float damper;
    [SerializeField, Range(0f, 300f)] private float massScale;
    
    public void SetSpring(bool enable)
    {
        if (enable)
        {
            if (SpringJoint == null)
            {
                SpringJoint = gameObject.AddComponent<SpringJoint>();
                SpringJoint.autoConfigureConnectedAnchor = false;
                SpringJoint.anchor = Vector3.zero;
                SpringJoint.connectedBody = null;
                SpringJoint.maxDistance = minDistance;
                SpringJoint.minDistance = maxDistance;
                SpringJoint.spring = spring;
                SpringJoint.damper = damper;
                SpringJoint.massScale = massScale;
                
            }
        }
        else
        {
            if (SpringJoint != null)
            {
                Destroy(SpringJoint);
                SpringJoint = null;
            }
        }

        Debug.Log($"SetSpring {(enable ? "ENABLED" : "DISABLED")}");
    }
}
