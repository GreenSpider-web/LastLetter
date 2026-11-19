// Об'єкт, який сам вирішує, що робити з предметом із сумки:
// адресат приймає лист (NPCAddressee), чайник відкриває вікно пари (SteamKettle).
// PlayerInteractor питає такий об'єкт першим. false — предмет не його,
// тоді працює звичайне Interactable.UseItem («Так не вийде» або потрібний предмет).
public interface IItemReceiver
{
    bool TryReceive(ItemData item);
}
