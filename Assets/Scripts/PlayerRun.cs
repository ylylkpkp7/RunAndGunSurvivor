using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRun : MonoBehaviour
{
    //横移動のx軸の限界
    const int MinLane = -2;
    const int MaxLane = 2;
    const float LaneWidth = 1.0f;

    //体力の最大値
    const int DefaultLife = 3;

    //ダメージのくらった時の硬直時間
    const float StunDuration = 0.5f;

    CharacterController controller;
    Animator animator;

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

    void OnMove(InputValue value)
    {
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
        //ジャンプに関するボタンを検知をしたらジャンプメソッド
        Jump();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

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

    }

    //Playerを硬直さるべきかチェックするメソッド
    public bool IsStun()
    {
        return recoverTime > 0 || life <= 0;
    }

    // Update is called once per frame
    void Update()
    {
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
    }

    public void MoveToleft()
    {
        //硬直フラグがtrueなら何もしない
        if (IsStun()) return;
        //地面にいる　かつ　targetがまだ最小ではない
        if (controller.isGrounded && targetLane > MinLane)
        {
            targetLane--;
            currentMoveInputX = 0; //何も入力していない状況にリセット
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
            targetLane++;
            currentMoveInputX = 0; //何も入力していない状況にリセット
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
        if(controller.isGrounded) //地面にいたら
        {
            moveDirection.y = speedJump;
        }
    }

    //CharacterControllerコンポーネントが何かとぶつかった時
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (IsStun()) return;

        //相手がEnemyなら
        if (hit.gameObject.tag == "Enemy")
        {
            life--;
            recoverTime = StunDuration; //recoverTimeに定数の値をセッティング

            Destroy(hit.gameObject); //相手は消滅
        }
    }
}




