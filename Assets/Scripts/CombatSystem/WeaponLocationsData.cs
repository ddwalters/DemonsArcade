using UnityEngine;

[CreateAssetMenu(fileName = "WeaponLocationsData", menuName = "Inventory/WeaponLocationsData")]
public class WeaponLocationsData : ScriptableObject
{
    [SerializeField] GameObject player;

    public GameObject GetRightHandLocation(WeaponType weaponType)
    {
        GameObject handLocation;
        switch (weaponType)
        {
            case WeaponType.ShortSword:
                handLocation = player.transform.Find("RightSword").gameObject;
                if (handLocation == null) return null;
                return handLocation;
            case WeaponType.Axe:
                handLocation = player.transform.Find("RightAxe").gameObject;
                if (handLocation == null) return null;
                return handLocation;
            case WeaponType.Shield:
                handLocation = player.transform.Find("RightShield").gameObject;
                if (handLocation == null) return null;
                return handLocation;
            case WeaponType.Staff:
                handLocation = player.transform.Find("RightStaff").gameObject;
                if (handLocation == null) return null;
                return handLocation;
            default:
                return null;
        }
    }

    public GameObject GetLeftHandLocation(WeaponType weaponType)
    {
        GameObject handLocation;
        switch (weaponType)
        {
            case WeaponType.ShortSword:
                handLocation = player.transform.Find("LeftSword").gameObject;
                if (handLocation == null) return null;
                return handLocation;
            case WeaponType.Axe:
                handLocation = player.transform.Find("LeftAxe").gameObject;
                if (handLocation == null) return null;
                return handLocation;
            case WeaponType.Shield:
                handLocation = player.transform.Find("LeftShield").gameObject;
                if (handLocation == null) return null;
                return handLocation;
            default:
                return null;
        }
    }
}