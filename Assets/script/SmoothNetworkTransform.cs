using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(PhotonView))]
public class SmoothNetworkTransform : MonoBehaviourPun, IPunObservable
{
    [Header("Đệm nội suy (giây)")]
    [Tooltip("Mạng Wi-Fi/4G nên để 0.14 - 0.16 để luôn đủ 2 gói nội suy, ít bị khựng khi jitter")]
    public float doTreNoiSuy = 0.15f;

    [Tooltip("Thời gian tối đa đoán tiếp vị trí & hướng khi gói đến trễ")]
    public float thoiGianDuDoanToiDa = 0.22f;

    [Tooltip("Lệch xa hơn mức này (mét) thì coi là hồi sinh / dịch chuyển và nhảy thẳng")]
    public float khoangCachDichChuyen = 6f;

    [Header("Độ bám quỹ đạo khi cua gấp")]
    [Range(0f, 1f)]
    [Tooltip("Tỷ lệ dùng đường cong Hermite (phần còn lại dùng Lerp thẳng để tránh vồng khi cua/phanh gấp)")]
    public float tyLeHermite = 0.72f;

    [Header("Vật lý trên máy người xem")]
    [Tooltip("Bật để Physics local không kéo giật xe người khác")]
    public bool khoaVatLyXeNguoiKhac = true;

    private struct Snap
    {
        public double t;
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 vel;
        public Vector3 angVel;
    }

    private const int SO_GOI_TOI_DA = 32;
    private const float THOI_GIAN_HAM_THEM = 0.15f;
    private readonly List<Snap> buf = new List<Snap>(SO_GOI_TOI_DA);

    private Rigidbody rb;
    private Vector3 viTriGuiTruoc;
    private Quaternion huongGuiTruoc = Quaternion.identity;
    private double thoiGianGuiTruoc;
    private bool daCoGuiTruoc;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (!photonView.IsMine && rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.None;
            if (khoaVatLyXeNguoiKhac)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }

    private void OnEnable()
    {
        buf.Clear();
        daCoGuiTruoc = false;
    }

    private static Vector3 LocNhieuVanToc(Vector3 v)
    {
        return v.sqrMagnitude < 0.05f * 0.05f ? Vector3.zero : v;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            double now = PhotonNetwork.Time;

            Vector3 vel = Vector3.zero;
            Vector3 angVel = Vector3.zero;

            if (rb != null && !rb.isKinematic)
            {
                vel = LocNhieuVanToc(rb.linearVelocity);
                angVel = LocNhieuVanToc(rb.angularVelocity);
            }
            else if (daCoGuiTruoc)
            {
                double dt = now - thoiGianGuiTruoc;
                if (dt > 0.0001)
                {
                    vel = LocNhieuVanToc((pos - viTriGuiTruoc) / (float)dt);
                    Quaternion delta = rot * Quaternion.Inverse(huongGuiTruoc);
                    delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
                    if (angleDeg > 180f) angleDeg -= 360f;
                    if (axis.sqrMagnitude > 0.0001f && Mathf.Abs(angleDeg) > 0.01f)
                        angVel = LocNhieuVanToc(axis.normalized * (angleDeg * Mathf.Deg2Rad / (float)dt));
                }
            }

            viTriGuiTruoc = pos;
            huongGuiTruoc = rot;
            thoiGianGuiTruoc = now;
            daCoGuiTruoc = true;

            stream.SendNext(pos);
            stream.SendNext(rot);
            stream.SendNext(vel);
            stream.SendNext(angVel);
        }
        else
        {
            Vector3 pos = (Vector3)stream.ReceiveNext();
            Quaternion rot = (Quaternion)stream.ReceiveNext();
            Vector3 vel = LocNhieuVanToc((Vector3)stream.ReceiveNext());
            Vector3 angVel = LocNhieuVanToc((Vector3)stream.ReceiveNext());

            ThemGoi(info.SentServerTime, pos, rot, vel, angVel);
        }
    }

    private void ThemGoi(double t, Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        if (buf.Count > 0)
        {
            Snap cuoi = buf[buf.Count - 1];
            if (t <= cuoi.t) return;

            bool dichChuyenXa = (pos - cuoi.pos).sqrMagnitude > khoangCachDichChuyen * khoangCachDichChuyen;
            bool dutKetNoiLau = (t - cuoi.t) > 1.5;

            if (dichChuyenXa || dutKetNoiLau)
            {
                buf.Clear();
                transform.SetPositionAndRotation(pos, rot);
            }
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }

        buf.Add(new Snap { t = t, pos = pos, rot = rot, vel = vel, angVel = angVel });
        if (buf.Count > SO_GOI_TOI_DA) buf.RemoveAt(0);
    }

    private void ApDungMucTieu(Vector3 targetPos, Quaternion targetRot, bool keoMuot)
    {
        float nguongSq = khoangCachDichChuyen * khoangCachDichChuyen;
        if (!keoMuot || (targetPos - transform.position).sqrMagnitude >= nguongSq)
        {
            transform.SetPositionAndRotation(targetPos, targetRot);
            return;
        }

        float k = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
        Vector3 posMoi = Vector3.Lerp(transform.position, targetPos, k);
        Quaternion rotMoi = Quaternion.Slerp(transform.rotation, targetRot, k);
        transform.SetPositionAndRotation(posMoi, rotMoi);
    }

    private void Update()
    {
        if (photonView.IsMine || buf.Count == 0) return;

        // Mới có 1 gói: bám theo gói đó mượt nhẹ, chưa đủ 2 mốc để nội suy
        if (buf.Count == 1)
        {
            Snap don = buf[0];
            ApDungMucTieu(don.pos, don.rot, true);
            return;
        }

        double renderT = PhotonNetwork.Time - doTreNoiSuy;

        while (buf.Count > 2 && buf[1].t <= renderT)
            buf.RemoveAt(0);

        Vector3 targetPos;
        Quaternion targetRot;
        bool keoMuot = false;

        Snap a = buf[0];
        Snap b = buf[1];

        if (renderT <= a.t)
        {
            targetPos = a.pos;
            targetRot = a.rot;
            keoMuot = true;
        }
        else if (renderT <= b.t)
        {
            float dt = (float)(b.t - a.t);
            float u = dt > 0.0001f ? Mathf.Clamp01((float)((renderT - a.t) / dt)) : 1f;

            Vector3 pHermite = Hermite(a.pos, a.vel * dt, b.pos, b.vel * dt, u);
            Vector3 pLerp = Vector3.Lerp(a.pos, b.pos, u);
            targetPos = Vector3.Lerp(pLerp, pHermite, tyLeHermite);
            targetRot = Quaternion.Slerp(a.rot, b.rot, u);
        }
        else
        {
            Snap moi = buf[buf.Count - 1];
            float lechThoiGian = Mathf.Max(0f, (float)(renderT - moi.t));
            float dtHieuLuc = Mathf.Min(lechThoiGian, thoiGianDuDoanToiDa + THOI_GIAN_HAM_THEM);

            float giamToc = Mathf.Exp(-2.5f * dtHieuLuc);
            if (lechThoiGian > thoiGianDuDoanToiDa)
            {
                float vuot = Mathf.Min(lechThoiGian - thoiGianDuDoanToiDa, THOI_GIAN_HAM_THEM);
                giamToc *= Mathf.Max(0f, 1f - vuot / THOI_GIAN_HAM_THEM);
            }

            targetPos = moi.pos + moi.vel * (dtHieuLuc * giamToc);

            float gocRad = moi.angVel.magnitude * (dtHieuLuc * giamToc);
            if (gocRad > 0.0001f && moi.angVel.sqrMagnitude > 0.0001f)
            {
                Quaternion qDoan = Quaternion.AngleAxis(gocRad * Mathf.Rad2Deg, moi.angVel.normalized);
                targetRot = qDoan * moi.rot;
            }
            else
            {
                targetRot = moi.rot;
            }
        }

        ApDungMucTieu(targetPos, targetRot, keoMuot);
    }

    private static Vector3 Hermite(Vector3 p0, Vector3 m0, Vector3 p1, Vector3 m1, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;
        float h11 = t3 - t2;
        return h00 * p0 + h10 * m0 + h01 * p1 + h11 * m1;
    }
}