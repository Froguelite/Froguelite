using System.Collections;
using UnityEngine;

public class StumpGoToLevel : MonoBehaviour
{


    private bool forceRight = false;

    #region OVERLAP


    public void OnOverlapGoToLevel()
    {
        TransitionToLevel();
    }

    private void TransitionToLevel()
    {
        //Supress await _=
        _= LevelManager.Instance.LoadScene(LevelManager.Scenes.MainScene, LevelManager.LoadEffect.LoadingScreen);
    }


    #endregion
}
