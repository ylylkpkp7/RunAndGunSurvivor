using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class NormalShooter : MonoBehaviour
{
    [Header("Bullet管理スクリプトと連携")]
    public BulletManager bulletManager;

    [Header("生成オブジェクトと位置")]
    public GameObject bulletPrefabs;//生成対象プレハブ
    public GameObject gate; //生成位置

    [Header("弾速")]
    public float shootSpeed = 10.0f; //弾速

    GameObject bullets; //生成した弾をまとめるオブジェクト

    const int maxShootPower = 3;　//最大威力
    int shootPower = 1;　//現在威力

    [Header("ソードのスクリプト")]
    public NormalSword normalSword;　//ソード中の動きを封じるため
    
    //InputAction(Playerマップ)のAttackアクションがおされたら
    void OnAttack(InputValue value)
    {
        //ソード中だったら何もしない
        if (normalSword.GetIsSword())
            return;
        //ゲームの状態がゲームオーバー、あるいはリトライだったら
        if(GameManager.gameState == GameState.retry)
        {
            //staticメソッドなので簡単に呼び出し
            GameManager.RetryScene();
        }
        else if (GameManager.gameState == GameState.result) //リザルト状態の時ならネクスト
        {
            //行先が自由に記述できるようにpublic変数を使っているので、NextSceneはStaticメソッドに行かず、地道に呼び出し
            GameManager gm = GameObject.FindGameObjectWithTag("GM").GetComponent<GameManager>();gm.NextScene(gm.nextScene);    
        }
        else　//ゲームステータスがプレイ中なら
        {
            Shoot();
        }
        
    }

    void Shoot()
    {
        //残数があれば
       if(bulletManager.GetBulletRemaining() > 0)
        {
            //プレハブの生成と生成情報の取得
            GameObject obj = Instantiate(
                bulletPrefabs, //何を
                gate.transform.position,//どこに
                Quaternion.Euler(90,0,0) //どの角度で
                );

            //生成したBulletをBulletsオブジェクトの子供にしてまとめる
            obj.transform.parent = bullets.transform;

            //bulletを消費
            bulletManager.ConsumeBullet();

            //生成したbullet自身のRigidbodyの力で飛ばす
            Rigidbody bulletRbody = obj.GetComponent<Rigidbody>();
            bulletRbody.AddForce(new Vector3(0,0,shootSpeed), ForceMode.Impulse);
        }
        else
        {
            //残数がなければマガジンを消費して補充開始
            bulletManager.RecoverBullet();
        }
    }

    void Start()
    {
        //指定したタグを持っているオブジェクトを取得
        bullets = GameObject.FindGameObjectWithTag("Bullets");
    }  
    
    public void ShootPowerUp()
    {
        shootPower++;//威力をあげる
        if (shootPower > maxShootPower) shootPower = maxShootPower;//最大威力までにおさえる
        GameObject canvas = GameObject.FindGameObjectWithTag("UI");　//UIタグの検索
        canvas.GetComponent<UIController>().UpdateGun();//UI更新
    }

    //威力が下がる
    public void ShootPowerDown()
    {
        shootPower--;//威力を下げる
        if (shootPower <= 0 ) shootPower = 1;//最小でも１
        GameObject canvas = GameObject.FindGameObjectWithTag("UI");　//UIタグの検索
        canvas.GetComponent<UIController>().UpdateGun();//UI更新
    }
    //現在の威力の取得
    public int GetShootPower()
    {
        return shootPower;
    }
}
