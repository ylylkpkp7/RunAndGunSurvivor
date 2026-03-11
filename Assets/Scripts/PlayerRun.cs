using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRun : MonoBehaviour
{
    //横移動のx軸の限界
    const int MinLane = -2; //最小レーン番号
    const int MaxLane = 2;　//最大レーン番号
    const float LaneWidth = 2.0f;　//レーン幅
    const int DefaultLife = 3;　//体力の最大値
　　const float StunDuration = 0.5f;　//硬直時間

    CharacterController controller;
    Animator animator;
    public GameObject animeBody; //アニメーターを持っている個体
    bool isAnime; //リトライ・リザルトのリアクションを発動させたか

    Vector3 moveDirection = Vector3.zero;　//移動すべき量
    int targetLane;　//向かうべきx座標
    int life = DefaultLife;　//現体力
    float recoverTime = 0.0f;　//復帰までのカウントダウン

    float currentMoveInputX;　//InputSystemの入力値を格納
    //Inputを連続で認知しないためのインターバルのコルーチン
    Coroutine resetIntervalCol;

    public float gravity = 20.0f;　//重力加速値
    public float speedZ = 5.0f;　//前進スピード
    public float speedX = 3.0f;　//横移動スピード
    public float speedJump = 8.0f;　//ジャンプ力
    public float accelerationZ = 10.0f;　//前進加速力

    [Header("ソードのスクリプト")]
    public NormalSword normalSword; //ソード中の動きを封じるため


    AudioSource[] playerAudio;
    //足音判定
    float footstepInterval = 0.3f; //足音間隔
    float footstepTimer; //時間計測
    [Header("SE音源")]
    public AudioClip se_Walk;
    public AudioClip se_Damage;
    public AudioClip se_Explosion;
    public AudioClip se_Jump;
    public AudioClip se_Dash;
    public AudioClip se_Reload;

    //現在の体力を返す
    public int Life()
    {
        return life;
    }

    //体力を1回復（DefaultLifeでバリエーション）
    public void LifeUP()
    {
        life++;
        if (life > DefaultLife) life = DefaultLife;
        GameObject canvas = GameObject.FindGameObjectWithTag("UI");
        canvas.GetComponent<UIController>().UpdateLife(Life());

    }

    //体力のダメージによる減少
    public void LifeDown()
    {
        life--;
        GameObject canvas = GameObject.FindGameObjectWithTag("UI");
        canvas.GetComponent<UIController>().UpdateLife(Life());

    }


    //Playerを硬直さるべきかチェックするメソッド
    public bool IsStun()
    {
        return recoverTime > 0 || life <= 0;
    }



    void OnMove(InputValue value)
    {
        //NormalSwordスクリプトのIsSword変数をみて攻撃中なら何もできない
        if (normalSword.GetIsSword()) return;
        //既に前に入力検知してインターバル中であれば何もしない
        if (resetIntervalCol == null)
        {
            //検知した値をVector2で表現して変数inputVectorに格納
            Vector2 inputVector = value.Get<Vector2>();
            //変数inputVectorのうち、ｘ座標にまつわる値を変数currentMoveInputXに格納
            currentMoveInputX = inputVector.x;
        }
    }

    void OnJump(InputValue value)
    {
        //NormalSwordスクリプトのIsSword変数をみて攻撃中なら何もできない
        if (normalSword.GetIsSword()) return;

        //ジャンプに関するボタンを検知をしたらジャンプメソッド
        Jump();
    }
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = animeBody.GetComponent<Animator>();
        playerAudio = GetComponents<AudioSource>();
    }

    void Update()
    {
        if (GameManager.gameState == GameState.stageclear ||
            GameManager.gameState == GameState.result) return;
        //InputManagerシステム採用の場合
        //if (Input.GetKeyDown("left")) MoveToLeft();
        //if (Input.GetKeyDoen("right")) MoveToRight();
        //if (Input.GetKeyDown("space")) Jump();

        //左押されていたら
        if (currentMoveInputX < 0) MoveToleft();

        //右押されていたら
        if (currentMoveInputX > 0) MoveToRight();

        if (IsStun()) //硬直フラグをチェック
        {
            //moveDirectionのxを0
            moveDirection.x = 0;
            //moveDirectionのzを0
            moveDirection.z = 0;
            //recoverTimeをカウントダウン
            recoverTime -= Time.deltaTime;
        }
        else
        {
            //前進のアルゴリズム
            //その時のmoveDirection.zにaccelerationZの加速度を足していく
            float acceleratedZ = moveDirection.z + (accelerationZ * Time.deltaTime);
            //導きだした値に上限を設けて、それをmoveDirection.zとする
            moveDirection.z = Mathf.Clamp(acceleratedZ, 0, speedZ);

            //横移動のアルゴリズム
            //目的と自分の位置の差を取り、1レーンあたりの幅に対して割合を見る
            float ratioX = (targetLane * LaneWidth - transform.position.x) / LaneWidth;
            //割合に変数speedXを係数としてかけた値がmoveDirection.x
            moveDirection.x = ratioX * speedX;
        }

        //重力の加速度をmoveDirection.y
        moveDirection.y -= gravity * Time.deltaTime;

        //回転時、自分にとってのZ軸をグローバル座標の値に変換
        Vector3 globalDirection = transform.TransformDirection(moveDirection);
        //CharacterControllerコンポーネントのMoveメソッドに授けてPlayerを動かす
        controller.Move(globalDirection * Time.deltaTime);

        //地面についていたら重力をリセット
        if (controller.isGrounded) moveDirection.y = 0;

        //足音メソッド
        HandleFootsteps();
    }

    //足音メソッド
    void HandleFootsteps()
    {
        //地面にいてプレイヤーが動いていれば
        if (controller.isGrounded && moveDirection.z != 0)
        {
            footstepTimer += Time.deltaTime; //時間計測

            if (footstepTimer >= footstepInterval) //インターバルチェック
            {
                playerAudio[1].PlayOneShot(se_Walk);
                footstepTimer = 0;
            }
        }
        else //動いていなければ時間計測リセット
        {
            footstepTimer = 0f;
        }
    }

    public void MoveToleft()
    {
        //硬直フラグがtrueなら何もしない
        if (IsStun()) return;
        //地面にいる　かつ　targetがまだ最小ではない
        if (controller.isGrounded && targetLane > MinLane)
        {
            playerAudio[0].PlayOneShot(se_Dash);
            targetLane--;
            //何も入力していない状況にリセット
            currentMoveInputX = 0;
            //次の入力検知を有効にするまでのインターバル
            resetIntervalCol = StartCoroutine(ResetIntervalCol());
        }
    }

    public void MoveToRight()
    {
        //硬直フラグがfalseなら何もしない
        if (IsStun()) return;
        //地面にいる　かつ　targetがまだ最大ではない
        if (controller.isGrounded && targetLane < MaxLane)
        {
            playerAudio[0].PlayOneShot(se_Dash);
            targetLane++;
            //何も入力していない状況にリセット
            currentMoveInputX = 0;
            //次の入力検知を有効にするまでのインターバル
            resetIntervalCol = StartCoroutine(ResetIntervalCol());
        }
    }

    IEnumerator ResetIntervalCol()
    {
        //とりあえず0.1秒待つ
        yield return new WaitForSeconds(0.1f);
        resetIntervalCol = null; //コルーチン情報を解除
    }

    public void Jump()
    {
        //硬直フラグがtrueなら何もしない
        if (IsStun()) return;
        if (controller.isGrounded) //地面にいたら
        {
            moveDirection.y = speedJump;
            animator.SetTrigger("jump");
            playerAudio[0].PlayOneShot(se_Jump);
        }
    }

    //CharacterControllerコンポーネントが何かとぶつかった時
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (IsStun()) return;

        //相手がEnemyなら
        if (hit.gameObject.tag == "Enemy")
        {
            playerAudio[2].PlayOneShot(se_Damage);
            LifeDown();//体力が減る
            GetComponent<NormalShooter>().ShootPowerDown();//銃の威力を減らすメソッド
            recoverTime = StunDuration; //recoverTimeに定数の値をセッティング

            //体力がなくなったらゲームオーバー
            if (life <= 0)
            {
                GameManager.gameState = GameState.gameover;
                if (!isAnime)
                {
                    animator.SetTrigger("retry");
                    isAnime = true;
                }
            }

            //Destroy(hit.gameObject); //相手は消滅
            hit.gameObject.GetComponent<Wall>().CreateEffect();
            animator.SetTrigger("damage");
        }
    }

    //ゴールに触れたらステータスをゲームクリアに変更
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Goal")
        {
            GameManager.gameState = GameState.stageclear;
            if (!isAnime)
            {
                animator.SetTrigger("result");
                isAnime = true;
                playerAudio[0].PlayOneShot(se_Reload);
            }
            Destroy(other.gameObject); //ゴールしたらゴールオブジェクトを抹消
        }
    }
}




