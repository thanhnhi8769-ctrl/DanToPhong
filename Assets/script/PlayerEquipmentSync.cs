using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerEquipmentSync : MonoBehaviourPunCallbacks
{
    [Header("Model theo thứ tự Shop")]
    public GameObject[] scooterModels;
    public GameObject[] helmetModels;
    public GameObject[] characterModels;

    [Header("Đội mũ lên đầu")]
    public bool tuDongDoiMuLenDau = true;
    public Vector3 doLechMuTrenDau = new Vector3(0f, 0.22f, 0.02f);

    [Header("Chuyển động lái xe tự nhiên")]
    public bool batChuyenDongLaiXe = true;
    [Tooltip("Trục (local của nhóm nhân vật) để nghiêng khi cua")]
    public Vector3 trucNghieng = Vector3.forward;
    [Tooltip("Trục (local của nhóm nhân vật) để chồm tới / ngả ra sau")]
    public Vector3 trucChom = Vector3.right;
    public float doNghiengKhiCua = 16f;
    public float doChomTangToc = 8f;
    public float doNgaKhiPhanh = 6f;
    public float nhipNhunKhiChay = 0.025f;
    public float tocDoNhun = 12f;
    public float doRungKhiChay = 0.004f;
    public float tocDoMaxThamChieu = 24f;

    [Header("Lò xo (độ mượt)")]
    public float doCung = 90f;
    public float doTat = 12f;

    [Header("Nhảy / tiếp đất")]
    public float doNhunTiepDat = 0.08f;

    private Rigidbody xeRb;
    private Transform nhomNhanVatTf, nhomMuTf, xuongDauHienTai;
    private Animator animatorHienTai;
    private bool animCoSpeed, animCoTurn;
    private Vector3 viTriGoc;
    private Quaternion gocGoc;
    private bool daKhoiTao;

    // Trạng thái lò xo
    private float nghieng, nghiengV;
    private float chom, chomV;
    private float nhunDoc, nhunDocV;

    // Dữ liệu frame trước
    private Vector3 viTriTruoc;
    private Vector3 vanTocMuot;
    private float vanTocXLocalTruoc;
    private float vanTocYTruoc;
    private float gocYTruoc;
    private float vanTocGocY;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashTurn = Animator.StringToHash("Turn");

    private void Awake()
    {
        xeRb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (photonView != null && photonView.IsMine)
        {
            int xe = PlayerInventory.GetEquippedIndex("Scooter");
            int mu = PlayerInventory.GetEquippedIndex("Helmet");
            int nv = PlayerInventory.GetEquippedIndex("Character");

            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
            {
                { "Scooter", xe }, { "Helmet", mu }, { "Character", nv }
            });
            ApDungChiSo(xe, mu, nv);
        }
        else if (photonView != null)
        {
            ApDungTrangBiTuPlayer(photonView.Owner);
        }

        KhoiTaoGocChuyenDong();
        viTriTruoc = transform.position;
        gocYTruoc = transform.eulerAngles.y;
    }

    private void KhoiTaoGocChuyenDong()
    {
        if (characterModels != null && characterModels.Length > 0 && characterModels[0] != null)
        {
            nhomNhanVatTf = characterModels[0].transform.parent;
            if (nhomNhanVatTf != null)
            {
                viTriGoc = nhomNhanVatTf.localPosition;
                gocGoc = nhomNhanVatTf.localRotation;
                daKhoiTao = true;
            }
        }

        if (helmetModels != null && helmetModels.Length > 0 && helmetModels[0] != null)
            nhomMuTf = helmetModels[0].transform.parent;
    }

    // Lò xo giảm chấn: mượt, có độ nảy tự nhiên
    private static float Spring(float hienTai, ref float vanToc, float mucTieu, float k, float c, float dt)
    {
        float luc = (mucTieu - hienTai) * k - vanToc * c;
        vanToc += luc * dt;
        return hienTai + vanToc * dt;
    }

    private void Update()
    {
        if (!batChuyenDongLaiXe || !daKhoiTao || nhomNhanVatTf == null) return;

        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (dt <= 0f) return;

        // --- Vận tốc: xe của mình dùng Rigidbody, xe remote tính từ vị trí ---
        Vector3 vanToc;
        if (xeRb != null && !xeRb.isKinematic)
        {
            vanToc = xeRb.linearVelocity;
        }
        else
        {
            Vector3 vTho = (transform.position - viTriTruoc) / dt;
            vanTocMuot = Vector3.Lerp(vanTocMuot, vTho, dt * 12f);
            vanToc = vanTocMuot;
        }
        viTriTruoc = transform.position;

        Vector3 vLocal = transform.InverseTransformDirection(vanToc);
        float tocDo = new Vector2(vanToc.x, vanToc.z).magnitude;
        float heSo = Mathf.Clamp01(tocDo / tocDoMaxThamChieu);

        // --- Tốc độ xoay thân xe (độ/giây): cho cả xe remote ---
        float gocY = transform.eulerAngles.y;
        float vGocTho = Mathf.DeltaAngle(gocYTruoc, gocY) / dt;
        vanTocGocY = Mathf.Lerp(vanTocGocY, vGocTho, dt * 10f);
        gocYTruoc = gocY;

        // --- Gia tốc dọc (xe chạy theo +X local) ---
        float giaTocDoc = (vLocal.x - vanTocXLocalTruoc) / dt;
        vanTocXLocalTruoc = vLocal.x;

        // --- Mục tiêu các lò xo ---
        float mucTieuNghieng = Mathf.Clamp(-vanTocGocY / 50f, -1f, 1f)
                               * doNghiengKhiCua * Mathf.Lerp(0.4f, 1f, heSo);

        float a = Mathf.Clamp(giaTocDoc / 20f, -1f, 1f);
        float mucTieuChom = heSo * 5f + (a > 0f ? a * doChomTangToc : a * doNgaKhiPhanh);

        // Nhảy / tiếp đất
        float dyV = vanToc.y - vanTocYTruoc;
        vanTocYTruoc = vanToc.y;
        if (dyV > 3f)
            nhunDocV += doNhunTiepDat * 30f;                       // vừa bật nhảy
        else if (dyV < -0.5f && Mathf.Abs(vanToc.y) < 0.5f)
            nhunDocV -= Mathf.Clamp(-dyV, 0f, 8f) * doNhunTiepDat * 10f; // vừa chạm đất

        nghieng = Spring(nghieng, ref nghiengV, mucTieuNghieng, doCung, doTat, dt);
        chom = Spring(chom, ref chomV, mucTieuChom, doCung, doTat, dt);
        nhunDoc = Spring(nhunDoc, ref nhunDocV, 0f, doCung * 1.4f, doTat * 0.9f, dt);

        // --- Nhún theo nhịp chạy + rung nhẹ ---
        float tanSo = Mathf.Lerp(2.5f, tocDoNhun, heSo);
        float bienDo = Mathf.Lerp(0.008f, nhipNhunKhiChay, heSo);
        float nhun = Mathf.Sin(Time.time * tanSo) * bienDo;
        float rung = (Mathf.PerlinNoise(Time.time * 18f, 0.3f) - 0.5f) * 2f * doRungKhiChay * heSo;

        nhomNhanVatTf.localPosition = viTriGoc + new Vector3(0f, nhun + rung + nhunDoc, 0f);
        nhomNhanVatTf.localRotation = gocGoc
            * Quaternion.AngleAxis(chom, trucChom)
            * Quaternion.AngleAxis(nghieng, trucNghieng);

        // --- Animator (chỉ set khi controller có tham số) ---
        if (animatorHienTai != null && animatorHienTai.isActiveAndEnabled)
        {
            if (animCoSpeed) animatorHienTai.SetFloat(HashSpeed, heSo, 0.1f, dt);
            if (animCoTurn) animatorHienTai.SetFloat(HashTurn, Mathf.Clamp(-vanTocGocY / 90f, -1f, 1f), 0.1f, dt);
        }
    }

    // Chạy sau Animator -> mũ bám đúng xương đầu
    private void LateUpdate()
    {
        if (!tuDongDoiMuLenDau || nhomMuTf == null || xuongDauHienTai == null) return;

        nhomMuTf.SetPositionAndRotation(
            xuongDauHienTai.TransformPoint(doLechMuTrenDau),
            xuongDauHienTai.rotation);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (photonView != null && targetPlayer == photonView.Owner)
            ApDungTrangBiTuPlayer(targetPlayer);
    }

    private void ApDungTrangBiTuPlayer(Player owner)
    {
        if (owner == null) return;

        var p = owner.CustomProperties;
        int xe = p.TryGetValue("Scooter", out object a) ? (int)a : 0;
        int mu = p.TryGetValue("Helmet", out object b) ? (int)b : 0;
        int nv = p.TryGetValue("Character", out object c) ? (int)c : 0;
        ApDungChiSo(xe, mu, nv);
    }

    private void ApDungChiSo(int xe, int mu, int nv)
    {
        BatModelTheoIndex(scooterModels, xe, false);
        BatModelTheoIndex(helmetModels, mu, false);
        BatModelTheoIndex(characterModels, nv, true);
    }

    private void BatModelTheoIndex(GameObject[] list, int activeIndex, bool laNhanVat)
    {
        if (list == null || list.Length == 0) return;
        activeIndex = Mathf.Clamp(activeIndex, 0, list.Length - 1);

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == null) continue;

            bool chon = (i == activeIndex);
            if (list[i].activeSelf != chon) list[i].SetActive(chon);
            if (!chon) continue;

            Transform cha = list[i].transform.parent;
            if (cha != null && !cha.gameObject.activeSelf) cha.gameObject.SetActive(true);

            if (laNhanVat) GanAnimatorVaXuongDau(list[i]);
        }
    }

    private void GanAnimatorVaXuongDau(GameObject nhanVat)
    {
        animatorHienTai = nhanVat.GetComponentInChildren<Animator>(true);
        xuongDauHienTai = null;
        animCoSpeed = animCoTurn = false;

        if (animatorHienTai != null)
        {
            animatorHienTai.enabled = true;
            animatorHienTai.applyRootMotion = false;

            if (animatorHienTai.isHuman)
                xuongDauHienTai = animatorHienTai.GetBoneTransform(HumanBodyBones.Head);

            if (animatorHienTai.runtimeAnimatorController != null)
            {
                foreach (var prm in animatorHienTai.parameters)
                {
                    if (prm.nameHash == HashSpeed) animCoSpeed = true;
                    else if (prm.nameHash == HashTurn) animCoTurn = true;
                }
            }
        }

        if (xuongDauHienTai == null)
            xuongDauHienTai = TimXuongDauTheoTen(nhanVat.transform);
    }

    private Transform TimXuongDauTheoTen(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            string ten = t.name.ToLowerInvariant();
            if (ten.Contains("head") && !ten.Contains("end") && !ten.Contains("top")) return t;
        }
        return root;
    }
}