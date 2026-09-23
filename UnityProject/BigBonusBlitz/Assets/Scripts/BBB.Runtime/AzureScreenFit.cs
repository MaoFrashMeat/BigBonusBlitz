using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>A 1170-wide modal can be opened from the legacy 960-wide title stage.</summary>
    public sealed class AzureScreenFit:MonoBehaviour
    {
        readonly Vector3[] corners=new Vector3[4];
        void LateUpdate()=>Apply();
        public void Apply()
        {
            var rt=(RectTransform)transform;var parent=rt.parent as RectTransform;if(parent==null)return;
            var safe=GetComponentInParent<SafeStage>();var canvas=GetComponentInParent<Canvas>();
            var bounds=safe!=null?safe.SafeRoot:canvas!=null?(RectTransform)canvas.transform:null;if(bounds==null)return;
            bounds.GetWorldCorners(corners);var lo=parent.InverseTransformPoint(corners[0]);var hi=parent.InverseTransformPoint(corners[2]);
            float scale=Mathf.Min(1f,Mathf.Min((hi.x-lo.x)/rt.sizeDelta.x,(hi.y-lo.y)/rt.sizeDelta.y));
            if(scale>0&&!float.IsNaN(scale))rt.localScale=Vector3.one*scale;
        }
    }
}
