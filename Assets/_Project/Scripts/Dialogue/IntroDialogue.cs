using System.Collections;
using UnityEngine;

// Вступ: щойно Ліля вперше з'являється на Пошті, Поштмейстер сам починає розмову.
// Один раз за гру — потім прапорець intro_done у GameState (зберігається в сейві).
public class IntroDialogue : MonoBehaviour
{
    [Tooltip("Вузол у LastLetter_Ch1.ink")]
    [SerializeField] private string knot = "postmaster_intro";
    [SerializeField] private string doneFlag = "intro_done";
    [Tooltip("Пауза після завантаження сцени, с — щоб спершу минуло затемнення")]
    [SerializeField] private float delay = 0.8f;

    private IEnumerator Start()
    {
        if (GameState.Instance == null || GameState.Instance.GetFlag(doneFlag)) yield break;

        yield return new WaitForSeconds(delay);
        if (DialogueManager.Instance == null) yield break;

        GameState.Instance.SetFlag(doneFlag, true);
        DialogueManager.Instance.StartKnot(knot);
    }
}
