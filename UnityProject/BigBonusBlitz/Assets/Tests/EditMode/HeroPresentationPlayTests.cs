#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Collections;
using BBB.Runtime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BBB.Tests
{
    public sealed class HeroPresentationPlayTests
    {
        [UnityTest]
        public IEnumerator PreviewAudioHonorsMutePauseAndOwnership()
        {
            if(Application.productName!="shop-qa"||!Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"))
                Assert.Ignore("Requires isolated QA project and preferences");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            var audio=AudioManager.Create();audio.SeVolume=.25f;
            var lab=HeroPresentationLab.Open();lab.enabled=false;
            var toggle=lab.Stage.Find("Inspector/PreviewSound").GetComponent<Button>();
            int before=audio.SoundSequence;
            lab.Request("guard");lab.Tick(.001f);
            Assert.AreEqual(before,audio.SoundSequence,"Preview is not silent by default");
            toggle.onClick.Invoke();
            Assert.AreEqual(.25f,audio.SeVolume,"Preview changed shared master volume");
            before=audio.SoundSequence;lab.Request("guard");lab.Tick(.001f);
            Assert.AreEqual(before+1,audio.SoundSequence,"Guard first frame did not dispatch exactly one SE");
            lab.Tick(.001f);Assert.AreEqual(before+1,audio.SoundSequence,"First frame duplicated SE");
            lab.Request("cast");lab.Paused=true;lab.Tick(1);
            Assert.AreEqual(before+1,audio.SoundSequence,"Paused preview emitted SE");
            lab.Paused=false;audio.SeVolume=0;before=audio.SoundSequence;lab.Tick(.3f);
            Assert.AreEqual(before,audio.SoundSequence,"Muted preview emitted SE");
            audio.SeVolume=.25f;toggle.onClick.Invoke();before=audio.SoundSequence;
            lab.Request("guard");lab.Tick(.001f);Assert.AreEqual(before,audio.SoundSequence,"Disabled preview emitted SE");
            lab.Close();yield return null;
            Assert.IsTrue(audio!=null,"Preview destroyed shared audio manager");
            Assert.AreEqual(.25f,audio.SeVolume);
            yield return new ExitPlayMode();
        }
    }
}
#endif
