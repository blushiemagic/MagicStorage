using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
    public class BiomeGlobeThrown : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Biome Globe");
            DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "生态球");
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.aiStyle = ProjAIStyleID.BeachBall;
            AIType = ProjectileID.BeachBall;
            Projectile.ignoreWater = false;
            Projectile.noDropItem = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //Need to mimick the bounce code from the beach ball since this isn't handled by the AI
            Projectile.ai[1] = 0f;
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = oldVelocity.X * -0.6f;

            if (Projectile.velocity.Y != oldVelocity.Y && oldVelocity.Y > 2f)
                Projectile.velocity.Y = oldVelocity.Y * -0.6f;

            return false;
        }
    }
}
