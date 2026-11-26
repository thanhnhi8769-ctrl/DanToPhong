using UnityEngine;

public class PlayerEquipmentSync : MonoBehaviour
{
    [Header("(Không còn dùng - có thể để trống)")]
    public GameObject[] scooterModels;

    private scooter xe;

    private void Start()
    {
        ApDungXeDaChon();
    }

    private void OnEnable()
    {
        if (xe != null) ApDungXeDaChon();
    }

    public void ApDungXeDaChon()
    {
        if (xe == null) xe = GetComponentInParent<scooter>();
        if (xe == null) return;

        xe.ApDungMauXe(VehicleCatalog.GetSelectedIndex());
    }
}