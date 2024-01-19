using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CharController : MonoBehaviour
{
	//Written by Diego "kiskidiego" Pérez Rueda
	private enum Directions
	{
		Right,
		Left,
		Up,
		Down,
		UpRight,
		UpLeft,
		DownRight,
		DownLeft
	}
    Rigidbody2D rigidBody;
	[SerializeField] float maxSpeed = 5f;
	[SerializeField] float acceleration = 5f;
	[SerializeField] float deceleration = 5f;
	[SerializeField] bool airTurn = true;
	[SerializeField] float airAcceleration = 0f;
	[SerializeField] float maxAirSpeed = 0f;
	[SerializeField] float airDeceleration = 0f;
	[SerializeField] float jumpForce = 5f;
	[SerializeField] int jumpExtensionFrames = 15;
	[SerializeField] int doubleJump = 0;
	[SerializeField] bool defaultDoubleJumpReset = true;
	[SerializeField] float doubleJumpForce = 0f;
	[SerializeField] int doubleJumpExtensionFrames = 15;
	[SerializeField] int dash = 0;
	[SerializeField] Directions[] dashDirections = new Directions[0];
	[SerializeField] bool dashOnlyInFacingDirection = false;
	[SerializeField] bool airDash = false;
	[SerializeField] bool defaultDashReset = true;
	[SerializeField] bool intangibleDash = false;
	[SerializeField] float dashSpeed = 0f;
	[SerializeField] int dashFrameDuration = 10;
	[SerializeField] float dashCooldown = 1f;
	[SerializeField] bool dashSpeedResetToZero = false;
	[SerializeField] bool dashSpeedResetToMaxSpeed = true;
	[SerializeField] bool wallJump = false;
	[SerializeField] float wallJumpVerticalForce = 0f;
	[SerializeField] float wallJumpHorizontalForce = 0f;
	[SerializeField] int wallJumpExtensionFrames = 15;
	[SerializeField] float wallJumpDuration = 0.2f;
	[SerializeField] bool wallSlide = false;
	[SerializeField] float maxSlideSpeed = 0f;
	[SerializeField] float slideAcceleration = 0f;
	[SerializeField] bool wallClimb = false;
	[SerializeField] float climbSpeed = 0f;
	[SerializeField] bool wallDash = false;
	[SerializeField] bool startLookingRight = true;
	[SerializeField] bool fastFall = false;
	[SerializeField] float fastFallSpeed = 100f;
	[SerializeField] float weight = 1f;


	Collider2D foot;
	Collider2D rightSide;
	Collider2D leftSide;
	
	private bool grounded => foot.IsTouchingLayers(LayerMask.GetMask("Terrain"));
	private bool rightTouching => rightSide.IsTouchingLayers(LayerMask.GetMask("Terrain"));
	private bool leftTouching => leftSide.IsTouchingLayers(LayerMask.GetMask("Terrain"));
	private int doubleJumpCount = 0;
	private int dashCount = 0;
	private Directions faceDirection = Directions.Right;
	private bool dashing = false;
	private bool canDash = true;
	private bool walltouching = false;
	private bool wallJumping = false;

	private void Start()
	{
		rigidBody = GetComponent<Rigidbody2D>();
		Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
		for(int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject.name == "Foot")
			{
				foot = colliders[i];
			}
			else if (colliders[i].gameObject.name == "RSide")
			{
				rightSide = colliders[i];
			}
			else if (colliders[i].gameObject.name == "LSide")
			{
				leftSide = colliders[i];
			}
		}
		if(!wallSlide)
		{
			wallClimb = false;
			wallJump = false;
			wallDash = false;
		}
		if(startLookingRight)
		{
			faceDirection = Directions.Right;
		}
		else
		{
			faceDirection = Directions.Left;
		}
	}
	void Update()
    {
		if (!dashing)
		{
			if (grounded)
			{

				// DEFAULT RESET JUMPS AND DASHES
				if (defaultDoubleJumpReset)
				{
					doubleJumpCount = 0;
				}
				if (defaultDashReset)
				{
					dashCount = 0;
				}


				// GROUNDED HORIZONTAL MOVEMENT
				if (Input.GetAxisRaw("Horizontal") > 0)
				{
					rigidBody.velocity = new Vector2(Mathf.MoveTowards(rigidBody.velocity.x, maxSpeed, acceleration * Time.deltaTime), rigidBody.velocity.y);
					faceDirection = Directions.Right;
				}
				else if (Input.GetAxisRaw("Horizontal") < 0)
				{
					rigidBody.velocity = new Vector2(Mathf.MoveTowards(rigidBody.velocity.x, -maxSpeed, acceleration * Time.deltaTime), rigidBody.velocity.y);
					faceDirection = Directions.Left;
				}
				else
				{
					rigidBody.velocity = new Vector2(Mathf.MoveTowards(rigidBody.velocity.x, 0, deceleration * Time.deltaTime), rigidBody.velocity.y);
				}


				// GROUNDED JUMP
				if (Input.GetButtonDown("Jump"))
				{
					rigidBody.velocity = new Vector2(rigidBody.velocity.x, jumpForce);
					StartCoroutine(JumpExtension(jumpForce));
				}


				// GROUNDED DASH
				if (Input.GetButtonDown("Dash") && dashCount < dash && !dashing && canDash)
				{
					StartCoroutine(DashTime());
					dashCount++;
				}
			}
			else // AIRBORNE
			{
				if (!wallJumping)
				{
					// AIRBORNE HORIZONTAL MOVEMENT
					if (Input.GetAxisRaw("Horizontal") > 0)
					{
						rigidBody.velocity = new Vector2(Mathf.MoveTowards(rigidBody.velocity.x, maxAirSpeed, airAcceleration * Time.deltaTime), rigidBody.velocity.y);
						if (airTurn)
						{
							faceDirection = Directions.Right;
						}
					}
					else if (Input.GetAxisRaw("Horizontal") < 0)
					{
						rigidBody.velocity = new Vector2(Mathf.MoveTowards(rigidBody.velocity.x, -maxAirSpeed, airAcceleration * Time.deltaTime), rigidBody.velocity.y);
						if (airTurn)
						{
							faceDirection = Directions.Left;
						}
					}
					else
					{
						rigidBody.velocity = new Vector2(Mathf.MoveTowards(rigidBody.velocity.x, 0, airDeceleration * Time.deltaTime), rigidBody.velocity.y);
					}
				}


				// AIRBORNE JUMP (DOUBLE JUMP)
				if (Input.GetButtonDown("Jump") && doubleJumpCount < doubleJump && !walltouching)
				{
					rigidBody.velocity = new Vector2(rigidBody.velocity.x, doubleJumpForce);
					StartCoroutine(DoubleJumpExtension(doubleJumpForce));
					doubleJumpCount++;
				}


				// AIRBORNE DASH
				if (Input.GetButtonDown("Dash") && dashCount < dash && airDash && !dashing && canDash && !walltouching)
				{
					StartCoroutine(DashTime());
					dashCount++;
				}


				// FAST FALL
				if (fastFall && !walltouching)
				{
					if (Input.GetAxisRaw("Vertical") < 0)
					{
						rigidBody.velocity = new Vector2(rigidBody.velocity.x, -fastFallSpeed);
					}
				}


				// WALL INTERACTION
				if (wallSlide)
				{
					if (rightTouching && Input.GetAxisRaw("Horizontal") > 0)
					{
						if(!walltouching)
						{
							rigidBody.velocity = new Vector2(rigidBody.velocity.x, Mathf.Clamp(rigidBody.velocity.y, 0, Mathf.Abs(rigidBody.velocity.y)));
							walltouching = true;
						}
						rigidBody.gravityScale = 0;
						rigidBody.velocity = new Vector2(rigidBody.velocity.x, Mathf.MoveTowards(rigidBody.velocity.y, -maxSlideSpeed, slideAcceleration * Time.deltaTime));
					}
					else if (leftTouching && Input.GetAxisRaw("Horizontal") < 0)
					{
						if (!walltouching)
						{
							rigidBody.velocity = new Vector2(rigidBody.velocity.x, Mathf.Clamp(rigidBody.velocity.y, 0, Mathf.Abs(rigidBody.velocity.y)));
							walltouching = true;
						}
						rigidBody.gravityScale = 0;
						rigidBody.velocity = new Vector2(rigidBody.velocity.x, Mathf.MoveTowards(rigidBody.velocity.y, -maxSlideSpeed, slideAcceleration * Time.deltaTime));
					}
					else
					{
						walltouching = false;
						rigidBody.gravityScale = weight;
					}
				}
				if(wallJump)
				{
					if(rightTouching && Input.GetAxisRaw("Horizontal") > 0)
					{
						if (Input.GetButtonDown("Jump"))
						{
							wallJumping = true;
							walltouching = false;
							rigidBody.velocity = new Vector2(-wallJumpHorizontalForce, wallJumpVerticalForce);
							StartCoroutine(WallJumpExtension(wallJumpVerticalForce));
							StartCoroutine(WallJumpTime());
						}
					}
					else if (leftTouching && Input.GetAxisRaw("Horizontal") < 0)
					{
						if (Input.GetButtonDown("Jump"))
						{
							wallJumping = true;
							walltouching = false;
							rigidBody.velocity = new Vector2(wallJumpHorizontalForce, wallJumpVerticalForce);
							StartCoroutine(WallJumpExtension(wallJumpVerticalForce));
							StartCoroutine(WallJumpTime());
						}
					}
				}
				if(wallDash)
				{
					if(walltouching && Input.GetButtonDown("Dash") && dashCount < dash && !dashing && canDash)
					{
						StartCoroutine(DashTime());
						dashCount++;
					}
				}
			}
			if (wallClimb && ! wallJumping)
			{
				if ((rightTouching && Input.GetAxisRaw("Horizontal") > 0) || (leftTouching && Input.GetAxisRaw("Horizontal") < 0))
				{
					if(!walltouching && !grounded)
					{
						rigidBody.velocity = new Vector2(rigidBody.velocity.x, Mathf.Clamp(rigidBody.velocity.y, 0, Mathf.Abs(rigidBody.velocity.y)));
						walltouching = true;
					}
					if (Input.GetAxisRaw("Vertical") > 0)
					{
						rigidBody.velocity = new Vector2(rigidBody.velocity.x, climbSpeed);
					}
					else if (Input.GetAxisRaw("Vertical") < 0)
					{
						rigidBody.velocity = new Vector2(rigidBody.velocity.x, -climbSpeed);
					}
				}
			}
		}
    }
	IEnumerator DashTime()
	{
		dashing = true;
		canDash = false;
		StartCoroutine(DashCooldown());
		if (intangibleDash)
		{
			gameObject.layer = LayerMask.NameToLayer("Intangible");
		}
		int i = 0;
		int horizontalDirection = 0;
		int verticalDirection = 0;
		Vector2 dashVector = new Vector2(0, 0);
		if (Input.GetAxisRaw("Horizontal") > 0)
		{
			horizontalDirection = 1;
		}
		else if (Input.GetAxisRaw("Horizontal") < 0)
		{
			horizontalDirection = -1;
		}
		if (Input.GetAxisRaw("Vertical") > 0)
		{
			verticalDirection = 1;
		}
		else if (Input.GetAxisRaw("Vertical") < 0)
		{
			verticalDirection = -1;
		}
		if (dashOnlyInFacingDirection)
		{
			if (faceDirection == Directions.Right)
			{
				if (horizontalDirection < 0)
				{
					horizontalDirection = 0;
				}
			}
			else if (faceDirection == Directions.Left)
			{
				if (horizontalDirection > 0)
				{
					horizontalDirection = 0;
				}
			}
		}
		if (walltouching)
		{
			if (wallDash)
			{
				if (faceDirection == Directions.Right)
				{
					horizontalDirection = -1;
				}
				else
				{
					horizontalDirection = 1;
				}
			}
		}
		bool right = false;
		bool left = false;
		bool up = false;
		bool down = false;
		bool upRight = false;
		bool upLeft = false;
		bool downRight = false;
		bool downLeft = false;
		for (int j = 0; j < dashDirections.Length; j++)
		{
			if (dashDirections[j] == Directions.Right)
			{
				right = true;
			}
			else if (dashDirections[j] == Directions.Left)
			{
				left = true;
			}
			else if (dashDirections[j] == Directions.Up)
			{
				up = true;
			}
			else if (dashDirections[j] == Directions.Down)
			{
				down = true;
			}
			else if (dashDirections[j] == Directions.UpRight)
			{
				upRight = true;
			}
			else if (dashDirections[j] == Directions.UpLeft)
			{
				upLeft = true;
			}
			else if (dashDirections[j] == Directions.DownRight)
			{
				downRight = true;
			}
			else if (dashDirections[j] == Directions.DownLeft)
			{
				downLeft = true;
			}
		}

		if (horizontalDirection > 0)
		{
			if (verticalDirection == 0)
			{
				if (right)
				{
					dashVector = new Vector2(dashSpeed, 0);
				}
				else
				{
					if (upRight)
					{
						dashVector = new Vector2(dashSpeed, dashSpeed).normalized * dashSpeed;
					}
					else if (downRight)
					{
						dashVector = new Vector2(dashSpeed, -dashSpeed).normalized * dashSpeed;
					}
					else if (up)
					{
						dashVector = new Vector2(0, dashSpeed);
					}
					else if (down)
					{
						dashVector = new Vector2(0, -dashSpeed);
					}
				}
			}
			else if (verticalDirection > 0)
			{
				if (upRight)
				{
					dashVector = new Vector2(dashSpeed, dashSpeed).normalized * dashSpeed;
				}
				else if (right)
				{
					dashVector = new Vector2(dashSpeed, 0);
				}
				else if (up)
				{
					dashVector = new Vector2(0, dashSpeed);
				}
			}
			else if (verticalDirection < 0)
			{
				if (downRight)
				{
					dashVector = new Vector2(dashSpeed, -dashSpeed).normalized * dashSpeed;
				}
				else if (right)
				{
					dashVector = new Vector2(dashSpeed, 0);
				}
				else if (down)
				{
					dashVector = new Vector2(0, -dashSpeed);
				}
			}
		}
		else if (horizontalDirection < 0)
		{
			if (verticalDirection == 0)
			{
				if (left)
				{
					dashVector = new Vector2(-dashSpeed, 0);
				}
				else
				{
					if (upLeft)
					{
						dashVector = new Vector2(-dashSpeed, dashSpeed).normalized * dashSpeed;
					}
					else if (downLeft)
					{
						dashVector = new Vector2(-dashSpeed, -dashSpeed).normalized * dashSpeed;
					}
					else if (up)
					{
						dashVector = new Vector2(0, dashSpeed);
					}
					else if (down)
					{
						dashVector = new Vector2(0, -dashSpeed);
					}
				}
			}
			else if (verticalDirection > 0)
			{
				if (upLeft)
				{
					dashVector = new Vector2(-dashSpeed, dashSpeed).normalized * dashSpeed;
				}
				else if (left)
				{
					dashVector = new Vector2(-dashSpeed, 0);
				}
				else if (up)
				{
					dashVector = new Vector2(0, dashSpeed);
				}
			}
			else if (verticalDirection < 0)
			{
				if (downLeft)
				{
					dashVector = new Vector2(-dashSpeed, -dashSpeed).normalized * dashSpeed;
				}
				else if (left)
				{
					dashVector = new Vector2(-dashSpeed, 0);
				}
				else if (down)
				{
					dashVector = new Vector2(0, -dashSpeed);
				}
			}
		}
		else if (horizontalDirection == 0)
		{
			if (verticalDirection > 0)
			{
				if (up)
				{
					dashVector = new Vector2(0, dashSpeed);
				}
				else
				{
					if (faceDirection == Directions.Right)
					{
						if (upRight)
						{
							dashVector = new Vector2(dashSpeed, dashSpeed).normalized * dashSpeed;
						}
						else if (upLeft)
						{
							dashVector = new Vector2(-dashSpeed, dashSpeed).normalized * dashSpeed;
						}
						else if (right)
						{
							dashVector = new Vector2(dashSpeed, 0);
						}
					}
					else if (faceDirection == Directions.Left)
					{
						if (upRight)
						{
							dashVector = new Vector2(dashSpeed, dashSpeed).normalized * dashSpeed;
						}
						else if (upLeft)
						{
							dashVector = new Vector2(-dashSpeed, dashSpeed).normalized * dashSpeed;
						}
						else if (left)
						{
							dashVector = new Vector2(-dashSpeed, 0);
						}
					}
				}
			}
			else if (verticalDirection < 0)
			{
				if (down)
				{
					dashVector = new Vector2(0, -dashSpeed);
				}
				else
				{
					if (faceDirection == Directions.Right)
					{
						if (downRight)
						{
							dashVector = new Vector2(dashSpeed, -dashSpeed).normalized * dashSpeed;
						}
						else if (downLeft)
						{
							dashVector = new Vector2(-dashSpeed, -dashSpeed).normalized * dashSpeed;
						}
						else if (right)
						{
							dashVector = new Vector2(dashSpeed, 0);
						}
					}
					else if (faceDirection == Directions.Left)
					{
						if (downRight)
						{
							dashVector = new Vector2(dashSpeed, -dashSpeed).normalized * dashSpeed;
						}
						else if (downLeft)
						{
							dashVector = new Vector2(-dashSpeed, -dashSpeed).normalized * dashSpeed;
						}
						else if (left)
						{
							dashVector = new Vector2(-dashSpeed, 0);
						}
					}
				}
			}
			else
			{
				if (faceDirection == Directions.Right)
				{
					if (right)
					{
						dashVector = new Vector2(dashSpeed, 0);
					}
					else if (upRight)
					{
						dashVector = new Vector2(dashSpeed, dashSpeed).normalized * dashSpeed;
					}
					else if (downRight)
					{
						dashVector = new Vector2(dashSpeed, -dashSpeed).normalized * dashSpeed;
					}
					else if (up)
					{
						dashVector = new Vector2(0, dashSpeed);
					}
					else if (down)
					{
						dashVector = new Vector2(0, -dashSpeed);
					}
				}
				else if (faceDirection == Directions.Left)
				{
					if (left)
					{
						dashVector = new Vector2(-dashSpeed, 0);
					}
					else if (upLeft)
					{
						dashVector = new Vector2(-dashSpeed, dashSpeed).normalized * dashSpeed;
					}
					else if (downLeft)
					{
						dashVector = new Vector2(-dashSpeed, -dashSpeed).normalized * dashSpeed;
					}
					else if (up)
					{
						dashVector = new Vector2(0, dashSpeed);
					}
					else if (down)
					{
						dashVector = new Vector2(0, -dashSpeed);
					}
				}
			}
		}

		while (i < dashFrameDuration)
		{
			i++;
			rigidBody.velocity = dashVector;
			yield return new WaitForFixedUpdate();
		}
		dashing = false;
		if (dashSpeedResetToZero)
		{
			rigidBody.velocity = new Vector2(0, 0);
		}
		else if (dashSpeedResetToMaxSpeed)
		{
			if (faceDirection == Directions.Right)
			{
				rigidBody.velocity = new Vector2(maxSpeed, 0);
			}
			else
			{
				rigidBody.velocity = new Vector2(-maxSpeed, 0);
			}
		}
		if (intangibleDash)
		{
			gameObject.layer = LayerMask.NameToLayer("Player");
		}
	}
	IEnumerator DashCooldown()
	{
		yield return new WaitForSeconds(dashCooldown);
		canDash = true;
	}
	IEnumerator WallJumpTime()
	{
		yield return new WaitForSeconds(wallJumpDuration);
		wallJumping = false;
	}
	IEnumerator JumpExtension(float force)
	{
		int i = 0;
		while (i < jumpExtensionFrames && Input.GetButton("Jump"))
		{
			i++;
			rigidBody.velocity = new Vector2(rigidBody.velocity.x, force);
			yield return new WaitForFixedUpdate();
		}
	}
	IEnumerator DoubleJumpExtension(float force)
	{
		int i = 0;
		while (i < jumpExtensionFrames && Input.GetButton("Jump"))
		{
			i++;
			rigidBody.velocity = new Vector2(rigidBody.velocity.x, force);
			yield return new WaitForFixedUpdate();
		}
	}
	IEnumerator WallJumpExtension(float force)
	{
		int i = 0;
		while (i < jumpExtensionFrames && Input.GetButton("Jump"))
		{
			i++;
			rigidBody.velocity = new Vector2(rigidBody.velocity.x, force);
			yield return new WaitForFixedUpdate();
		}
	}
}