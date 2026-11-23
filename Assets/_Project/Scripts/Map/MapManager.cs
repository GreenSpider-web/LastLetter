using UnityEngine;

// 6 районів Межі (GDD → M5 «Мапа Межі»)
public enum District { PostSquare, Bridge, TailorStreet, SortingRoom, Trenches, BridgePost }

// «Туман» — ще чекають; «Світло» — мешканець пішов за міст; «Зникло» — лише контур на папері
public enum DistrictState { Fog, Light, Gone }

// Стани районів. Окремо їх не зберігаємо: вони обчислюються з того, що вже є в збереженні
// (доставлені листи в LetterManager і прапорці GameState). Тому після «Продовжити» мапа одразу правильна.
// У зрізі змінюються Площа Пошти, Міст і Вулиця Ниток; Сортувальна, Окопи й Пост на мосту — глава 2.   // [CHANGED]
// На мапі «Світло» = район пройдено (можна йти далі): він без туману й світло-жовтий (UI_Map).            // [NEW]
public static class MapManager
{
    public static readonly District[] All =
    {
        District.PostSquare, District.Bridge, District.TailorStreet,
        District.SortingRoom, District.Trenches, District.BridgePost
    };

    public static DistrictState GetState(District district)
    {
        switch (district)
        {
            case District.PostSquare:       // [NEW] Поштмейстер відправив в арку — далі Вулиця Ниток
                return Flag("arch_open") ? DistrictState.Light : DistrictState.Fog;
            case District.Bridge:           // Пантелеймон прочитав лист і пішов
                return IsDelivered("fisher") || Flag("prologue_done") ? DistrictState.Light : DistrictState.Fog;
            case District.TailorStreet:     // пані Стефанія пішла
                return Flag("tailor_left") ? DistrictState.Light : DistrictState.Fog;
            default:
                return DistrictState.Fog;
        }
    }

    public static string Name(District district)
    {
        switch (district)
        {
            case District.PostSquare: return "Площа Пошти";
            case District.Bridge: return "Міст";
            case District.TailorStreet: return "Вулиця Ниток";
            case District.SortingRoom: return "Сортувальна";
            case District.Trenches: return "Окопи";
            default: return "Пост на мосту";
        }
    }

    public static string StateName(DistrictState state)
    {
        switch (state)
        {
            case DistrictState.Light: return "Світло";
            case DistrictState.Gone: return "Зникло";
            default: return "Туман";
        }
    }

    // У якому районі зараз Ліля (за назвою сцени)
    public static District Current(string sceneName)
    {
        if (sceneName.Contains("Tailor")) return District.TailorStreet;   // L1_TailorStreet, L1_TailorHouse
        return District.PostSquare;                                       // L1_PostSquare і пісочниці
    }

    private static bool Flag(string name) => GameState.Instance != null && GameState.Instance.GetFlag(name);

    private static bool IsDelivered(string letterId)
    {
        if (LetterManager.Instance == null) return false;
        foreach (string id in LetterManager.Instance.DeliveredIds)
            if (id == letterId) return true;
        return false;
    }
}
