using UnityEngine;
using VoidEater.Hole;

namespace VoidEater.Player
{
    public sealed class PlayerHole : HoleBase
    {
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField] private Vector2 mapHalfExtents = new Vector2(45f, 45f);

        private Vector2 moveInput;

        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        private void Update()
        {
            SetMoveInput(ReadKeyboardInput());
        }

        private void FixedUpdate()
        {
            Vector3 delta = new Vector3(moveInput.x, 0f, moveInput.y) * (moveSpeed * Time.fixedDeltaTime);
            Vector3 nextPosition = Body.position + delta;

            nextPosition.x = Mathf.Clamp(nextPosition.x, -mapHalfExtents.x, mapHalfExtents.x);
            nextPosition.z = Mathf.Clamp(nextPosition.z, -mapHalfExtents.y, mapHalfExtents.y);

            Body.MovePosition(nextPosition);
        }

        private static Vector2 ReadKeyboardInput()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                x -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                x += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                y -= 1f;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                y += 1f;
            }

            return new Vector2(x, y);
        }
    }
}
