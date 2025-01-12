using UnityEngine;

public class Items : MonoBehaviour
{   
    public int points = 10;

    private void Take()
    {
        FindObjectOfType<GameManager>().ItemTaken(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Thief")) {
            Take();
            gameObject.SetActive(false);
        }
    }
}
