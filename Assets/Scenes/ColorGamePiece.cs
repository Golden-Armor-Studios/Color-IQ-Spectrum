using UnityEngine;

public class ColorGamePiece
{
    private const string CircleSpritePath = "Circle";
    private const string Level2TilePath = "Sprites/level-2-tile";
    private const float PixelsPerUnit = 100f;

    private readonly GameObject srGameObject;

    public ColorGamePiece(Vector3 position, Color selectedColor, int level, float gamePieceSize, float gamePieceRadius, bool isLevelTile)
    {
        if (!isLevelTile)
        {
            Texture2D texture = Resources.Load<Texture2D>(CircleSpritePath);
            if (texture == null)
            {
                Debug.LogError($"Failed to load circle texture at Resources/{CircleSpritePath}");
                srGameObject = new GameObject("MissingCircleTexture");
                srGameObject.transform.position = position;
                return;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0.0f, 0.0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );
            sprite.name = "circle";

            srGameObject = new GameObject(selectedColor.ToColorKey());
            srGameObject.tag = "GamePiece";

            SpriteRenderer renderer = srGameObject.AddComponent<SpriteRenderer>();
            renderer.color = selectedColor;
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Simple;

            srGameObject.transform.position = position;
            float baseWidth = renderer.sprite.bounds.size.x;
            float baseHeight = renderer.sprite.bounds.size.y;
            float scaleX = gamePieceSize / baseWidth;
            float scaleY = gamePieceSize / baseHeight;
            srGameObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            CircleCollider2D collider = srGameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = false;
            float maxScale = Mathf.Max(scaleX, scaleY);
            collider.radius = (gamePieceSize * 0.5f) / maxScale;
        }
        else
        {
            GameObject prefab = Resources.Load<GameObject>(Level2TilePath);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load level 2 tile prefab at Resources/{Level2TilePath}");
                srGameObject = new GameObject("MissingLevel2Tile");
                srGameObject.transform.position = position;
                return;
            }

            srGameObject = Object.Instantiate(prefab);
            srGameObject.name = selectedColor.ToColorKey();
            srGameObject.tag = "GamePiece_Level_2";
            srGameObject.transform.position = position;
            srGameObject.transform.rotation = Quaternion.identity;
            srGameObject.transform.localScale = Vector3.one;

            SpriteRenderer renderer = srGameObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = selectedColor;
                renderer.drawMode = SpriteDrawMode.Simple;
                renderer.sortingOrder = 1;

                float baseWidth = renderer.sprite.bounds.size.x;
                float baseHeight = renderer.sprite.bounds.size.y;
                float scaleX = gamePieceSize / baseWidth;
                float scaleY = gamePieceSize / baseHeight;
                srGameObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            }

            CircleCollider2D colliderComponent = srGameObject.GetComponent<CircleCollider2D>();
            if (colliderComponent == null)
            {
                colliderComponent = srGameObject.AddComponent<CircleCollider2D>();
            }
            colliderComponent.isTrigger = false;
            float levelTileScale = Mathf.Max(srGameObject.transform.localScale.x, srGameObject.transform.localScale.y);
            colliderComponent.radius = (gamePieceSize * 0.5f) / levelTileScale;

            Rigidbody2D rigidBody = srGameObject.GetComponent<Rigidbody2D>();
            if (rigidBody != null)
            {
                rigidBody.linearVelocity = Vector2.zero;
                rigidBody.angularVelocity = 0f;
                rigidBody.gravityScale = 0f;
                rigidBody.bodyType = RigidbodyType2D.Kinematic;
                rigidBody.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }
    }

    public GameObject Render()
    {
        return srGameObject;
    }
}
