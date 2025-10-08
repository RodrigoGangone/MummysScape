using UnityEngine;

public class SwingHandler : MonoBehaviour
{
    public SpringJoint SpringJoint { get; private set; }

    [SerializeField, Range(0f, 5f)] private float minDistance = 1.25f;
    [SerializeField, Range(0f, 5f)] private float maxDistance = 2.50f;
    [SerializeField, Range(0f, 200f)] private float spring = 50f;
    [SerializeField, Range(0f, 30f)] private float damper = 18f;
    [SerializeField, Range(0f, 300f)] private float massScale = 150f;
    
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
