﻿using Microsoft.Xna.Framework;
using Midori.Core;
using Midori.GameObjects.Projectiles;
using Midori.Interfaces;
using System;
using System.Diagnostics;
using Midori.GameObjects.Units.Enemies;

namespace Midori.GameObjects.Units
{
    public abstract class Unit : AnimatedGameObject, IUnit
    {
        private const int UnitGravity = 13;
        private const int UnitConsequentJumps = 2;

        private int health;
        private int maxHealth;
        private float movementSpeed;
        private float defaultMovementSpeed;
        private float jumpSpeed;
        private float defaultJumpSpeed;
        private int jumpCounter;
        private int damageRanged;
        private Rectangle futurePosition;

        protected Unit()
            : base()
        {
            this.Timer = 0.0;
            this.CurrentFrame = 0;
            this.SourceRect = new Rectangle();

            this.JumpCounter = 0;

            this.IsJumping = false;
            this.IsFalling = false;
            this.IsMovingLeft = false;
            this.IsMovingRight = false; 
            this.HasFreePathing = false;

            this.IsFacingLeft = false;

            this.IsAttackingRanged = false;
        }

        # region Properties

        // IDestroyable
        public int Health
        {
            get { return this.health; }
            protected set
            {
                if (value > this.MaxHealth)
                {
                    this.health = this.MaxHealth;   
                }
                else if (value < 0)
                {
                    this.health = 0;
                }
                else
                {
                    this.health = value;
                }                
            }
        }

        public int MaxHealth
        {
            get { return this.maxHealth; }
            protected set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("Max health should not be negative or zero");
                }

                this.maxHealth = value;
            }
        }

        // IMovable
        public float MovementSpeed
        {
            get { return this.movementSpeed; }
            protected set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Movement speed should not be negative");
                }
                
                this.movementSpeed = value;
            }
        }

        public float DefaultMovementSpeed
        {
            get { return this.defaultMovementSpeed; }
            protected set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Default movement speed should not be negative");
                }

                this.defaultMovementSpeed = value;
            }
        }

        // IJumper
        public float JumpSpeed
        {
            get { return this.jumpSpeed; }
            protected set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Jump speed should not be negative");
                }

                this.jumpSpeed = value;
            }
        }

        public float DefaultJumpSpeed
        {
            get { return this.defaultJumpSpeed; }
            protected set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Default jump speed should not be negative");
                }

                this.defaultJumpSpeed = value;
            }
        }

        // IMultiJumper
        public int JumpCounter
        {
            get { return this.jumpCounter; }
            protected set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Jump counter should not be negative");
                }

                this.jumpCounter = value;
            }
        }
        
        // INeedToKnowEhereImFacing
        public bool IsFacingLeft { get; protected set; }

        // IRangedAttacker
        public bool IsAttackingRanged { get; protected set; }

        public int DamageRanged
        {
            get { return this.damageRanged; }
            protected set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Ranged damage should not be negative");
                }

                this.damageRanged = value;
            }
        }
        
        // IUnit
        public bool IsMovingRight { get; protected set; }

        public bool IsMovingLeft { get; protected set; }

        public bool IsJumping { get; protected set; }

        public bool IsFalling { get; protected set; }

        public bool HasFreePathing { get; protected set; }

        public Rectangle FuturePosition
        {
            get { return this.futurePosition; }
            protected set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Future position shouldn't be null");
                }

                this.futurePosition = value;
            }
        }


# endregion 

        # region Non-abstract Methods

        // Non-abstract Methods
        protected void ManageMovement(GameTime gameTime)
        {
            
            if (this.IsJumping)
            {
                if (this.ValidateUpperPosition())
                {
                    // if upper position is valid continue jumping
                    this.Y -= this.JumpSpeed;
                    this.JumpSpeed--;
                }
                else
                {
                    // else stop the jump; start falling
                    this.JumpSpeed = 0;
                }

                // if jump is over
                if (this.JumpSpeed == 0)
                {
                    // if unit is inside a tile => gain free pathing
                    if (Collision.CheckForCollisionWithPlatforms(this.BoundingBox))
                    {
                        this.HasFreePathing = true;
                    }
                    this.IsJumping = false;
                    this.IsFalling = true;
                }
            }
            else if (this.IsFalling)
            {
                if (this.HasFreePathing)
                { 
                    if (!Collision.CheckForCollisionWithAnyTiles(this.BoundingBox))                    
                    {
                        // if unit is already outside a tile => lose free pathing
                        this.HasFreePathing = false;
                    }
                    else
                    {
                        // if unit is still inside a tile => fall
                        this.ApplyGravity();
                    }
                }
                else
                {
                    if (!this.ValidateLowerPosition())
                    {
                        if (!Collision.CheckForCollisionWithAnyTiles(this.BoundingBox))
                        {
                            // if the lower position is invalid and the current is valid => unit is on ground
                            this.ApplyOnGroundEffect();
                        }
                        else
                        {
                            // fall to avoid getting stuck in a tile (side entry bug)
                            this.ApplyGravity();
                        }
                    }
                    else
                    {
                        // if the lower position is valid => fall
                        this.ApplyGravity();                      
                    }
                }
            }
            else
            {
                if (this.ValidateLowerPosition())
                {
                    // if the lower position is valid
                    this.IsFalling = true;
                }
            }

            // Left & Right Movement
            if (this.IsMovingLeft)
            {
                this.FuturePosition = new Rectangle(
                            (int)(this.BoundingBox.X - this.MovementSpeed),
                            (int)this.BoundingBox.Y,
                            this.BoundingBox.Width,
                            this.BoundingBox.Height);

                if ((this.HasFreePathing || Collision.CheckForCollisionWithAnyTiles(this.BoundingBox))
                    && !Collision.CheckForCollisionWithWalls(this.FuturePosition))
                {
                    Debug.WriteLine("here");
                    // if unit has free pathing OR is in a tile
                    // AND will not collide with a wall => move
                    this.X -= this.MovementSpeed;
                }
                else
                {

                    Debug.WriteLine("there");
                    if (!Collision.CheckForCollisionWithAnyTiles(this.FuturePosition))
                    {
                        Debug.WriteLine("there1");
                        // if next position is valid => move
                        this.X -= this.MovementSpeed;
                    }
                    else
                    {
                        Debug.WriteLine("there2");
                        // else => stop
                        this.IsMovingLeft = false;
                    }
                }
                
            }
            else if (this.IsMovingRight)
            {
                this.FuturePosition = new Rectangle(
                                (int)(this.BoundingBox.X + this.MovementSpeed),
                                (int)this.BoundingBox.Y,
                                this.BoundingBox.Width,
                                this.BoundingBox.Height);

                if ((this.HasFreePathing || Collision.CheckForCollisionWithAnyTiles(this.BoundingBox))
                    && !Collision.CheckForCollisionWithWalls(this.FuturePosition))
                {
                    // if unit has free pathing OR is in a tile
                    // AND will not collide with a wall => move
                    this.X += this.MovementSpeed;
                }
                else
                {                    
                    if (!Collision.CheckForCollisionWithAnyTiles(this.FuturePosition))
                    {
                        // if next position is valid => move
                        this.X += this.MovementSpeed;
                    }
                    else
                    {
                        // else => stop
                        this.IsMovingRight = false;
                    }
                }
                
            }

            // Return from opposite side if left the field
            if (Collision.CheckForCollisionWithWorldBounds(this))
            {
                ReturnFromOppositeSide();
            }

        }

        // returns true if the next position (by gravity pull) is valid
        private bool ValidateLowerPosition()
        {
            this.FuturePosition = new Rectangle(
                (int)this.BoundingBox.X,
                (int)(this.BoundingBox.Y + UnitGravity),
                this.BoundingBox.Width,
                this.BoundingBox.Height);
            if (Collision.CheckForCollisionWithAnyTiles(this.FuturePosition))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool ValidateUpperPosition()
        {
            this.FuturePosition = new Rectangle(
                (int)this.BoundingBox.X,
                (int)(this.BoundingBox.Y - this.JumpSpeed),
                this.BoundingBox.Width,
                this.BoundingBox.Height);
            if (Collision.CheckForCollisionWithOtherThanPlatform(this.FuturePosition))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void ApplyGravity()
        {
            this.Y += UnitGravity;
        }

        private void ApplyOnGroundEffect()
        {
            this.IsFalling = false;
            this.HasFreePathing = false;
            this.JumpCounter = 0;
        }

        private void ReturnFromOppositeSide()
        {
            if (this.BoundingBoxX > Engine.LevelBounds.Width)
            {
                this.X = -this.BoundingBox.Width;
            }
            if (this.BoundingBoxX < -this.BoundingBox.Width)
            {
                this.X = Engine.LevelBounds.Width - 5;
            }
            if (this.BoundingBoxY > Engine.LevelBounds.Height)
            {
                this.Y = -70;
            }
        }

        public void ResetAnimation()
        {
            this.CurrentFrame = 0;
            this.Timer = 0.0;
        }

        public void MakeUnitIdle()
        {
            this.IsMovingRight = false;
            this.IsMovingLeft = false;
        }

		public void GetHitByProjectile(Projectile projectile)
		{
			if (projectile.AbleToDoDamage)
			{
				if (projectile is RayParticle)
				{
					this.Health -= 3;
				}
				else
				{
					this.Health -= projectile.Owner.DamageRanged;
				}
			}
		}

        # endregion

        # region Abstract Methods

        // Abstract Methods
        public abstract void Update(GameTime gameTime);

        protected abstract void UpdateBoundingBox();

        protected abstract void ManageAnimation(GameTime gameTime);

        # endregion
    }
}
