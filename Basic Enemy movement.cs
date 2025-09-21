using UnityEngine;

public class BasicEnemymovement : MonoBehaviour
{
   // private Vector2[] positions;
   // private int Indexspot = 0;
    private Transform target;
    public float speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        //code for patrolling/random movement
        //positions = new Vector2[2];
        //positions[0] = new Vector2(transform.position.x , transform.position.y + 3);
        //positions[1] = new Vector2(transform.position.x , transform.position.y - 3);
    }

    // Update is called once per frame
    void Update()
    {
       // if(Vector2.Distance(transform.position, target) > 0.1f){
     
        if(Vector2.Distance(transform.position, target.position) > 0.1f){
          transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        }
        else{
             //If touching player, attack player
        }
        //}
        //else{
            //Indictaor on when to damage player
            //Indexspot = Indexspot + 1;
            //if(Indexspot > positions.Length - 1)
            //    Indexspot = 0;
        //}
    }
}
