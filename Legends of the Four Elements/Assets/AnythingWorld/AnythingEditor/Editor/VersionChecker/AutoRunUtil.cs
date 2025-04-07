#if UNITY_EDITOR
using UnityEditor;
namespace AnythingWorld.Editor
{
    [InitializeOnLoad]
    public class AutoRunUtil
    {
        static AutoRunUtil()
        {
            if(!SessionState.GetBool("AutoRunUtilCalled", false))
            {
                EditorApplication.update += RunOnce;
                SessionState.SetBool("AutoRunUtilCalled", true);
            }
        }

        static void RunOnce()
        {
            VersionCheckEditor.TryGetUpdateDialogue();
            EditorApplication.update -= RunOnce;

        }
    }
}
#endif

