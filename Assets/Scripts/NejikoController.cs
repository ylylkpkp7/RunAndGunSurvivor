using UnityEngine;

public class NejikoController : MonoBehaviour
{
    CharacterController controller; //キャラクターの移動管理
   // Animator animator;

    Vector3 moveDirection = Vector3.zero;　//移動するべき量　Vector3.zero→new Vector3(0,0,0)

    public float gravity;　//重力加速度
    public float speedZ;　//前進する力
    public float speedJump;　//ジャンプ力

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //必要なコンポーネントを自動取得
        controller = GetComponent<CharacterController>();
        //animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (controller.isGrounded) //CharacterControllerコンポーネントが持っている接地のチェック（bool)
        {
            //垂直方向のボタン入力をチェック（Vertical ↑　↓　WS）
            if (Input.GetAxis("Vertical") > 0.0f)
            {
                //このフレームにおける前進/後退の移動量が決まる
                moveDirection.z = Input.GetAxis("Vertical") * speedZ;
            }
            else
            {
                moveDirection.z = 0;
            }

            //左右キーを押したときの回転
            transform.Rotate(0, Input.GetAxis("Horizontal") * 3, 0);

            if (Input.GetButton("Jump"))　//スペースキー
            {
                moveDirection.y = speedJump;
                //animator.SetTrigger("jump");
            }
        }
        //ここまででそのフレームの移動するべき量が決まる（moveDirectionのxとy)
        
        //重力を考慮
        moveDirection.y -= gravity * Time.deltaTime;

        //引数に与えたVector3値を、そのオブジェクトの向きに合わせてグローバルな値として何が正しいかに変換
        Vector3 globalDirection = transform.TransformDirection(moveDirection);
        //Moveメソッドに与えたVector3値分だけ実際にPlayerが働く
        controller.Move(globalDirection * Time.deltaTime);

        //移動後接地してたらY方向の速度をリセットする
        if (controller.isGrounded) moveDirection.y = 0;

        //速度が0以上なら走っているフラグをtrueにする
        //animator.SetBool("run", moveDirection.z > 0.0f);
        
    }
}
