using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;
using ConVar;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("MercuryUtilites", "Mercury", "0.0.1")]
    class MercuryUtilites : RustPlugin
    {
        public Dictionary<ulong, string> HexTakePlayer = new Dictionary<ulong, string>();

        #region Vars

        #region IconsRust
        public HashSet<string> IconsRust = new HashSet<string>
        {
            "assets/bundled/prefabs/fx/lightning/fx-lightning.png",
            "assets/bundled/prefabs/fx/lightning/fx-lightning2.png",
            "assets/bundled/prefabs/fx/lightning/fx-lightning3.png",
            "assets/bundled/prefabs/fx/lightning/fx-lightning4.png",
            "assets/content/effects/muzzleflashes/muzzle_flash-cross.tga",
            "assets/content/effects/muzzleflashes/muzzle_flash-front-3x3.tga",
            "assets/content/effects/muzzleflashes/muzzle_flash-ring.tga",
            "assets/content/effects/muzzleflashes/muzzle_flash-side-1x2.tga",
            "assets/content/effects/muzzleflashes/muzzle_flash-side-1x4.tga",
            "assets/content/effects/muzzleflashes/muzzle_flash-t.tga",
            "assets/content/effects/muzzleflashes/muzzle_flash-v.tga",
            "assets/content/effects/muzzleflashes/muzzle_smoketrail.psd",
            "assets/content/image effects/lens dirt/lensdirt1.png",
            "assets/content/image effects/lens dirt/lensdirt10.png",
            "assets/content/image effects/lens dirt/lensdirt11.png",
            "assets/content/image effects/lens dirt/lensdirt12.png",
            "assets/content/image effects/lens dirt/lensdirt13.png",
            "assets/content/image effects/lens dirt/lensdirt14.png",
            "assets/content/image effects/lens dirt/lensdirt15.png",
            "assets/content/image effects/lens dirt/lensdirt16.png",
            "assets/content/image effects/lens dirt/lensdirt2.png",
            "assets/content/image effects/lens dirt/lensdirt3.png",
            "assets/content/image effects/lens dirt/lensdirt4.png",
            "assets/content/image effects/lens dirt/lensdirt5.png",
            "assets/content/image effects/lens dirt/lensdirt6.png",
            "assets/content/image effects/lens dirt/lensdirt7.png",
            "assets/content/image effects/lens dirt/lensdirt8.png",
            "assets/content/image effects/lens dirt/lensdirt9.png",
            "assets/content/materials/highlight.png",
            "assets/content/textures/generic/fulltransparent.tga",
            "assets/content/ui/developer/developmentskin/devpanelbg.png",
            "assets/content/ui/developer/developmentskin/devtab-active.png",
            "assets/content/ui/developer/developmentskin/devtab-bright.png",
            "assets/content/ui/developer/developmentskin/devtab-normal.png",
            "assets/content/ui/facepunch-darkbg.png",
            "assets/content/ui/gameui/compass/alpha_mask.png",
            "assets/content/ui/gameui/compass/compass_strip.png",
            "assets/content/ui/gameui/compass/compass_strip_hd.png",
            "assets/content/ui/gameui/ui.crosshair.circle.png",
            "assets/content/ui/gameui/underlays/ui.damage.directional.normal.tga",
            "assets/content/ui/hypnotized.png",
            "assets/content/ui/menuui/rustlogo-blurred.png",
            "assets/content/ui/menuui/rustlogo-normal-transparent.png",
            "assets/content/ui/menuui/ui.loading.logo.tga",
            "assets/content/ui/menuui/ui.logo.big.png",
            "assets/content/ui/menuui/ui.menu.logo.png",
            "assets/content/ui/menuui/ui.menu.news.missingbackground.jpg",
            "assets/content/ui/menuui/ui.menu.rateus.background.png",
            "assets/content/ui/overlay_binocular.png",
            "assets/content/ui/overlay_bleeding.png",
            "assets/content/ui/overlay_bleeding_height.tga",
            "assets/content/ui/overlay_bleeding_normal.tga",
            "assets/content/ui/overlay_freezing.png",
            "assets/content/ui/overlay_goggle.png",
            "assets/content/ui/overlay_helmet_slit.png",
            "assets/content/ui/overlay_poisoned.png",
            "assets/content/ui/overlay_scope_1.png",
            "assets/content/ui/overlay_scope_2.png",
            "assets/content/ui/tiledpatterns/circles.png",
            "assets/content/ui/tiledpatterns/stripe_reallythick.png",
            "assets/content/ui/tiledpatterns/stripe_slight.png",
            "assets/content/ui/tiledpatterns/stripe_slight_thick.png",
            "assets/content/ui/tiledpatterns/stripe_thick.png",
            "assets/content/ui/tiledpatterns/stripe_thin.png",
            "assets/content/ui/tiledpatterns/swirl_pattern.png",
            "assets/content/ui/ui.background.gradient.psd",
            "assets/content/ui/ui.background.tile.psd",
            "assets/content/ui/ui.background.tiletex.psd",
            "assets/content/ui/ui.background.transparent.linear.psd",
            "assets/content/ui/ui.background.transparent.linearltr.tga",
            "assets/content/ui/ui.background.transparent.radial.psd",
            "assets/content/ui/ui.icon.rust.png",
            "assets/content/ui/ui.serverimage.default.psd",
            "assets/content/ui/ui.spashscreen.psd",
            "assets/content/ui/ui.white.tga",
            "assets/icons/add.png",
            "assets/icons/ammunition.png",
            "assets/icons/arrow_right.png",
            "assets/icons/authorize.png",
            "assets/icons/bite.png",
            "assets/icons/bleeding.png",
            "assets/icons/blueprint.png",
            "assets/icons/blueprint_underlay.png",
            "assets/icons/blunt.png",
            "assets/icons/bp-lock.png",
            "assets/icons/broadcast.png",
            "assets/icons/build/stairs.png",
            "assets/icons/build/wall.doorway.door.png",
            "assets/icons/build/wall.window.bars.png",
            "assets/icons/bullet.png",
            "assets/icons/cargo_ship_body.png",
            "assets/icons/cart.png",
            "assets/icons/change_code.png",
            "assets/icons/check.png",
            "assets/icons/chinook_map_blades.png",
            "assets/icons/chinook_map_body.png",
            "assets/icons/circle_closed.png",
            "assets/icons/circle_closed_toedge.png",
            "assets/icons/circle_gradient.png",
            "assets/icons/circle_gradient_white.png",
            "assets/icons/circle_open.png",
            "assets/icons/clear.png",
            "assets/icons/clear_list.png",
            "assets/icons/close.png",
            "assets/icons/close_door.png",
            "assets/icons/clothing.png",
            "assets/icons/cold.png",
            "assets/icons/community_servers.png",
            "assets/icons/connection.png",
            "assets/icons/construction.png",
            "assets/icons/cooking.png",
            "assets/icons/crate.png",
            "assets/icons/cup_water.png",
            "assets/icons/cursor-hand.png",
            "assets/icons/deauthorize.png",
            "assets/icons/demolish.png",
            "assets/icons/demolish_cancel.png",
            "assets/icons/demolish_immediate.png",
            "assets/icons/discord 1.png",
            "assets/icons/discord.png",
            "assets/icons/download.png",
            "assets/icons/drop.png",
            "assets/icons/drowning.png",
            "assets/icons/eat.png",
            "assets/icons/electric.png",
            "assets/icons/embrella.png",
            "assets/icons/enter.png",
            "assets/icons/examine.png",
            "assets/icons/exit.png",
            "assets/icons/explosion.png",
            "assets/icons/explosion_sprite.png",
            "assets/icons/extinguish.png",
            "assets/icons/facebook-box.png",
            "assets/icons/facebook.png",
            "assets/icons/facepunch.png",
            "assets/icons/fall.png",
            "assets/icons/favourite_servers.png",
            "assets/icons/file.png",
            "assets/icons/flags/af.png",
            "assets/icons/flags/ar.png",
            "assets/icons/flags/ca.png",
            "assets/icons/flags/cs.png",
            "assets/icons/flags/da.png",
            "assets/icons/flags/de.png",
            "assets/icons/flags/el.png",
            "assets/icons/flags/en-pt.png",
            "assets/icons/flags/en.png",
            "assets/icons/flags/es-es.png",
            "assets/icons/flags/fi.png",
            "assets/icons/flags/fr.png",
            "assets/icons/flags/he.png",
            "assets/icons/flags/hu.png",
            "assets/icons/flags/it.png",
            "assets/icons/flags/ja.png",
            "assets/icons/flags/ko.png",
            "assets/icons/flags/nl.png",
            "assets/icons/flags/no.png",
            "assets/icons/flags/pl.png",
            "assets/icons/flags/pt-br.png",
            "assets/icons/flags/pt-pt.png",
            "assets/icons/flags/ro.png",
            "assets/icons/flags/ru.png",
            "assets/icons/flags/sr.png",
            "assets/icons/flags/sv-se.png",
            "assets/icons/flags/tr.png",
            "assets/icons/flags/uk.png",
            "assets/icons/flags/vi.png",
            "assets/icons/flags/zh-cn.png",
            "assets/icons/flags/zh-tw.png",
            "assets/icons/fog.png",
            "assets/icons/folder.png",
            "assets/icons/folder_up.png",
            "assets/icons/fork_and_spoon.png",
            "assets/icons/freezing.png",
            "assets/icons/friends_servers.png",
            "assets/icons/gear.png",
            "assets/icons/grenade.png",
            "assets/icons/greyout.png",
            "assets/icons/greyout_large.png",
            "assets/icons/health.png",
            "assets/icons/history_servers.png",
            "assets/icons/home.png",
            "assets/icons/horse_ride.png",
            "assets/icons/hot.png",
            "assets/icons/ignite.png",
            "assets/icons/info.png",
            "assets/icons/inventory.png",
            "assets/icons/isbroken.png",
            "assets/icons/iscooking.png",
            "assets/icons/isloading.png",
            "assets/icons/isonfire.png",
            "assets/icons/joystick.png",
            "assets/icons/key.png",
            "assets/icons/knock_door.png",
            "assets/icons/lan_servers.png",
            "assets/icons/level.png",
            "assets/icons/level_metal.png",
            "assets/icons/level_stone.png",
            "assets/icons/level_top.png",
            "assets/icons/level_wood.png",
            "assets/icons/lick.png",
            "assets/icons/light_campfire.png",
            "assets/icons/lightbulb.png",
            "assets/icons/loading.png",
            "assets/icons/lock.png",
            "assets/icons/loot.png",
            "assets/icons/maparrow.png",
            "assets/icons/market.png",
            "assets/icons/maximum.png",
            "assets/icons/meat.png",
            "assets/icons/medical.png",
            "assets/icons/menu_dots.png",
            "assets/icons/modded_servers.png",
            "assets/icons/occupied.png",
            "assets/icons/open.png",
            "assets/icons/open_door.png",
            "assets/icons/peace.png",
            "assets/icons/pickup.png",
            "assets/icons/pills.png",
            "assets/icons/player_assist.png",
            "assets/icons/player_carry.png",
            "assets/icons/player_loot.png",
            "assets/icons/poison.png",
            "assets/icons/portion.png",
            "assets/icons/power.png",
            "assets/icons/press.png",
            "assets/icons/radiation.png",
            "assets/icons/rain.png",
            "assets/icons/reddit.png",
            "assets/icons/refresh.png",
            "assets/icons/resource.png",
            "assets/icons/rotate.png",
            "assets/icons/rust.png",
            "assets/icons/save.png",
            "assets/icons/shadow.png",
            "assets/icons/sign.png",
            "assets/icons/slash.png",
            "assets/icons/sleeping.png",
            "assets/icons/sleepingbag.png",
            "assets/icons/square.png",
            "assets/icons/square_gradient.png",
            "assets/icons/stab.png",
            "assets/icons/star.png",
            "assets/icons/steam.png",
            "assets/icons/steam_inventory.png",
            "assets/icons/stopwatch.png",
            "assets/icons/store.png",
            "assets/icons/study.png",
            "assets/icons/subtract.png",
            "assets/icons/target.png",
            "assets/icons/tools.png",
            "assets/icons/translate.png",
            "assets/icons/traps.png",
            "assets/icons/triangle.png",
            "assets/icons/tweeter.png",
            "assets/icons/twitter 1.png",
            "assets/icons/twitter.png",
            "assets/icons/unlock.png",
            "assets/icons/upgrade.png",
            "assets/icons/voice.png",
            "assets/icons/vote_down.png",
            "assets/icons/vote_up.png",
            "assets/icons/warning.png",
            "assets/icons/warning_2.png",
            "assets/icons/weapon.png",
            "assets/icons/web.png",
            "assets/icons/wet.png",
            "assets/icons/workshop.png",
            "assets/icons/xp.png",
            "assets/prefabs/building core/floor.frame/floor.frame.png",
            "assets/prefabs/building core/floor.triangle/floor.triangle.png",
            "assets/prefabs/building core/floor/floor.png",
            "assets/prefabs/building core/foundation.steps/foundation.steps.png",
            "assets/prefabs/building core/foundation.triangle/foundation.triangle.png",
            "assets/prefabs/building core/foundation/foundation.png",
            "assets/prefabs/building core/roof/roof.png",
            "assets/prefabs/building core/stairs.l/stairs_l.png",
            "assets/prefabs/building core/stairs.u/stairs_u.png",
            "assets/prefabs/building core/wall.doorway/wall.doorway.png",
            "assets/prefabs/building core/wall.frame/wall.frame.png",
            "assets/prefabs/building core/wall.half/wall.half.png",
            "assets/prefabs/building core/wall.low/wall.third.png",
            "assets/prefabs/building core/wall.window/wall.window.png",
            "assets/prefabs/building core/wall/wall.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bossform0.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bossform0_grey.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bossform1.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bossform1_grey.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bossform2.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bossform2_grey.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bullet1.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bullet2.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bullet3.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bullet4.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bullet5.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bullet6.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bullet7.png",
            "assets/prefabs/misc/chippy arcade/chippyart/bullet8.png",
            "assets/prefabs/misc/chippy arcade/chippyart/chippy.png",
            "assets/prefabs/misc/chippy arcade/chippyart/chippy_grey.png",
            "assets/prefabs/misc/chippy arcade/chippyart/chippylogo.png",
            "assets/prefabs/misc/chippy arcade/chippyart/cloud1.png",
            "assets/prefabs/misc/chippy arcade/chippyart/cloud2.png",
            "assets/prefabs/misc/chippy arcade/chippyart/cloud3.png",
            "assets/prefabs/misc/chippy arcade/chippyart/cloud4.png",
            "assets/prefabs/misc/chippy arcade/chippyart/cloud5.png",
            "assets/prefabs/misc/chippy arcade/chippyart/grid.png",
            "assets/prefabs/misc/chippy arcade/chippyart/shield.png",
            "assets/prefabs/misc/chippy arcade/chippyart/shield_pickup.png",
            "assets/prefabs/misc/chippy arcade/chippyart/star1.png",
            "assets/prefabs/misc/chippy arcade/chippyart/star2.png",
            "assets/standard assets/effects/imageeffects/textures/color correction ramp.png",
            "assets/standard assets/effects/imageeffects/textures/contrastenhanced3d16.png",
            "assets/standard assets/effects/imageeffects/textures/grayscale ramp.png",
            "assets/standard assets/effects/imageeffects/textures/hexshape.psd",
            "assets/standard assets/effects/imageeffects/textures/motionblurjitter.png",
            "assets/standard assets/effects/imageeffects/textures/neutral3d16.png",
            "assets/standard assets/effects/imageeffects/textures/noise.png",
            "assets/standard assets/effects/imageeffects/textures/noiseandgrain.png",
            "assets/standard assets/effects/imageeffects/textures/noiseeffectgrain.png",
            "assets/standard assets/effects/imageeffects/textures/noiseeffectscratch.png",
            "assets/standard assets/effects/imageeffects/textures/randomvectors.png",
            "assets/standard assets/effects/imageeffects/textures/sphereshape.psd",
            "assets/standard assets/effects/imageeffects/textures/vignettemask.png",
            "assets/scenes/prefabs/airfield/airfield_1/alphatexture.png",
            "assets/scenes/prefabs/airfield/airfield_1/blendtexture.png",
            "assets/scenes/prefabs/airfield/airfield_1/heighttexture.png",
            "assets/scenes/prefabs/airfield/airfield_1/normaltexture.png",
            "assets/scenes/prefabs/airfield/airfield_1/splattexture0.png",
            "assets/scenes/prefabs/airfield/airfield_1/splattexture1.png",
            "assets/scenes/prefabs/airfield/airfield_1/topologytexture.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/alphatexture.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/bandit_town_biometexture.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/bandit_town_splat_texture_0.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/bandit_town_splat_texture_1.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/biometexture.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/blendtexture.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/heighttexture.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/normaltexture.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/splattexture0.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/splattexture1.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/topologytexture.png",
            "assets/scenes/prefabs/bandid_town/bandit_town/watertexture.png",
            "assets/scenes/prefabs/canyons/canyon_1/alphatexture.png",
            "assets/scenes/prefabs/canyons/canyon_1/heighttexture.png",
            "assets/scenes/prefabs/canyons/canyon_1/normaltexture.png",
            "assets/scenes/prefabs/canyons/canyon_1/splattexture0.png",
            "assets/scenes/prefabs/canyons/canyon_1/splattexture1.png",
            "assets/scenes/prefabs/canyons/canyon_1/topologytexture.png",
            "assets/scenes/prefabs/canyons/canyon_2/alphatexture.png",
            "assets/scenes/prefabs/canyons/canyon_2/heighttexture.png",
            "assets/scenes/prefabs/canyons/canyon_2/normaltexture.png",
            "assets/scenes/prefabs/canyons/canyon_2/splattexture0.png",
            "assets/scenes/prefabs/canyons/canyon_2/splattexture1.png",
            "assets/scenes/prefabs/canyons/canyon_2/topologytexture.png",
            "assets/scenes/prefabs/canyons/canyon_3/alphatexture.png",
            "assets/scenes/prefabs/canyons/canyon_3/heighttexture.png",
            "assets/scenes/prefabs/canyons/canyon_3/normaltexture.png",
            "assets/scenes/prefabs/canyons/canyon_3/splattexture0.png",
            "assets/scenes/prefabs/canyons/canyon_3/splattexture1.png",
            "assets/scenes/prefabs/canyons/canyon_3/topologytexture.png",
            "assets/scenes/prefabs/canyons/canyon_4/alphatexture.png",
            "assets/scenes/prefabs/canyons/canyon_4/heighttexture.png",
            "assets/scenes/prefabs/canyons/canyon_4/normaltexture.png",
            "assets/scenes/prefabs/canyons/canyon_4/splattexture0.png",
            "assets/scenes/prefabs/canyons/canyon_4/splattexture1.png",
            "assets/scenes/prefabs/canyons/canyon_4/topologytexture.png",
            "assets/scenes/prefabs/canyons/canyon_5/alphatexture.png",
            "assets/scenes/prefabs/canyons/canyon_5/heighttexture.png",
            "assets/scenes/prefabs/canyons/canyon_5/normaltexture.png",
            "assets/scenes/prefabs/canyons/canyon_5/splattexture0.png",
            "assets/scenes/prefabs/canyons/canyon_5/splattexture1.png",
            "assets/scenes/prefabs/canyons/canyon_5/topologytexture.png",
            "assets/scenes/prefabs/canyons/canyon_6/alphatexture.png",
            "assets/scenes/prefabs/canyons/canyon_6/heighttexture.png",
            "assets/scenes/prefabs/canyons/canyon_6/normaltexture.png",
            "assets/scenes/prefabs/canyons/canyon_6/splattexture0.png",
            "assets/scenes/prefabs/canyons/canyon_6/splattexture1.png",
            "assets/scenes/prefabs/canyons/canyon_6/topologytexture.png",
            "assets/scenes/prefabs/cave/cave_large_hard/alphatexture.png",
            "assets/scenes/prefabs/cave/cave_large_hard/blendtexture.png",
            "assets/scenes/prefabs/cave/cave_large_hard/heighttexture.png",
            "assets/scenes/prefabs/cave/cave_large_hard/normaltexture.png",
            "assets/scenes/prefabs/cave/cave_large_hard/splattexture0.png",
            "assets/scenes/prefabs/cave/cave_large_hard/splattexture1.png",
            "assets/scenes/prefabs/cave/cave_large_hard/topologytexture.png",
            "assets/scenes/prefabs/cave/cave_large_medium/alphatexture.png",
            "assets/scenes/prefabs/cave/cave_large_medium/blendtexture.png",
            "assets/scenes/prefabs/cave/cave_large_medium/heighttexture.png",
            "assets/scenes/prefabs/cave/cave_large_medium/normaltexture.png",
            "assets/scenes/prefabs/cave/cave_large_medium/splattexture0.png",
            "assets/scenes/prefabs/cave/cave_large_medium/splattexture1.png",
            "assets/scenes/prefabs/cave/cave_large_medium/topologytexture.png",
            "assets/scenes/prefabs/cave/cave_large_sewers_hard/alphatexture.png",
            "assets/scenes/prefabs/cave/cave_large_sewers_hard/blendtexture.png",
            "assets/scenes/prefabs/cave/cave_large_sewers_hard/heighttexture.png",
            "assets/scenes/prefabs/cave/cave_large_sewers_hard/normaltexture.png",
            "assets/scenes/prefabs/cave/cave_large_sewers_hard/splattexture0.png",
            "assets/scenes/prefabs/cave/cave_large_sewers_hard/splattexture1.png",
            "assets/scenes/prefabs/cave/cave_large_sewers_hard/topologytexture.png",
            "assets/scenes/prefabs/cave/cave_medium_easy/alphatexture.png",
            "assets/scenes/prefabs/cave/cave_medium_easy/blendtexture.png",
            "assets/scenes/prefabs/cave/cave_medium_easy/heighttexture.png",
            "assets/scenes/prefabs/cave/cave_medium_easy/normaltexture.png",
            "assets/scenes/prefabs/cave/cave_medium_easy/splattexture0.png",
            "assets/scenes/prefabs/cave/cave_medium_easy/splattexture1.png",
            "assets/scenes/prefabs/cave/cave_medium_easy/topologytexture.png",
            "assets/scenes/prefabs/cave/cave_medium_hard/alphatexture.png",
            "assets/scenes/prefabs/cave/cave_medium_hard/blendtexture.png",
            "assets/scenes/prefabs/cave/cave_medium_hard/heighttexture.png",
            "assets/scenes/prefabs/cave/cave_medium_hard/normaltexture.png",
            "assets/scenes/prefabs/cave/cave_medium_hard/splattexture0.png",
            "assets/scenes/prefabs/cave/cave_medium_hard/splattexture1.png",
            "assets/scenes/prefabs/cave/cave_medium_hard/topologytexture.png",
            "assets/scenes/prefabs/cave/cave_medium_medium/alphatexture.png",
            "assets/scenes/prefabs/cave/cave_medium_medium/biometexture.png",
            "assets/scenes/prefabs/cave/cave_medium_medium/blendtexture.png",
            "assets/scenes/prefabs/cave/cave_medium_medium/heighttexture.png",
            "assets/scenes/prefabs/cave/cave_medium_medium/normaltexture.png",
            "assets/scenes/prefabs/cave/cave_medium_medium/splattexture0.png",
            "assets/scenes/prefabs/cave/cave_medium_medium/splattexture1.png",
            "assets/scenes/prefabs/cave/cave_medium_medium/topologytexture.png",
            "assets/scenes/prefabs/cave/cave_small_easy/alphatexture.png",
            "assets/scenes/prefabs/cave/cave_small_easy/blendtexture.png",
            "assets/scenes/prefabs/cave/cave_small_easy/heighttexture.png",
            "assets/scenes/prefabs/cave/cave_small_easy/normaltexture.png",
            "assets/scenes/prefabs/cave/cave_small_easy/splattexture0.png",
            "assets/scenes/prefabs/cave/cave_small_easy/splattexture1.png",
            "assets/scenes/prefabs/cave/cave_small_easy/topologytexture.png",
            "assets/scenes/prefabs/cave/cave_small_hard/alphatexture.png",
            "assets/scenes/prefabs/cave/cave_small_hard/blendtexture.png",
            "assets/scenes/prefabs/cave/cave_small_hard/heighttexture.png",
            "assets/scenes/prefabs/cave/cave_small_hard/normaltexture.png",
            "assets/scenes/prefabs/cave/cave_small_hard/splattexture0.png",
            "assets/scenes/prefabs/cave/cave_small_hard/splattexture1.png",
            "assets/scenes/prefabs/cave/cave_small_hard/topologytexture.png",
            "assets/scenes/prefabs/cave/cave_small_medium/alphatexture.png",
            "assets/scenes/prefabs/cave/cave_small_medium/blendtexture.png",
            "assets/scenes/prefabs/cave/cave_small_medium/heighttexture.png",
            "assets/scenes/prefabs/cave/cave_small_medium/normaltexture.png",
            "assets/scenes/prefabs/cave/cave_small_medium/splattexture0.png",
            "assets/scenes/prefabs/cave/cave_small_medium/splattexture1.png",
            "assets/scenes/prefabs/cave/cave_small_medium/topologytexture.png",
            "assets/scenes/prefabs/compound/compound/alphatexture.png",
            "assets/scenes/prefabs/compound/compound/blendtexture.png",
            "assets/scenes/prefabs/compound/compound/heighttexture.png",
            "assets/scenes/prefabs/compound/compound/normaltexture.png",
            "assets/scenes/prefabs/compound/compound/splattexture0.png",
            "assets/scenes/prefabs/compound/compound/splattexture1.png",
            "assets/scenes/prefabs/compound/compound/topologytexture.png",
            "assets/scenes/prefabs/excavator/excavator/alphatexture.png",
            "assets/scenes/prefabs/excavator/excavator/biometexture.png",
            "assets/scenes/prefabs/excavator/excavator/blendtexture.png",
            "assets/scenes/prefabs/excavator/excavator/heighttexture.png",
            "assets/scenes/prefabs/excavator/excavator/normaltexture.png",
            "assets/scenes/prefabs/excavator/excavator/splattexture0.png",
            "assets/scenes/prefabs/excavator/excavator/splattexture1.png",
            "assets/scenes/prefabs/excavator/excavator/terrain_clean_biometexture.png",
            "assets/scenes/prefabs/excavator/excavator/terrain_clean_splat_texture_0.png",
            "assets/scenes/prefabs/excavator/excavator/terrain_clean_splat_texture_1.png",
            "assets/scenes/prefabs/excavator/excavator/terrain_clean_terrain_height.png",
            "assets/scenes/prefabs/excavator/excavator/topologytexture.png",
            "assets/scenes/prefabs/excavator/excavator/watertexture.png",
            "assets/scenes/prefabs/gas_station/gas_station/alphatexture.png",
            "assets/scenes/prefabs/gas_station/gas_station/biometexture.png",
            "assets/scenes/prefabs/gas_station/gas_station/blendtexture.png",
            "assets/scenes/prefabs/gas_station/gas_station/heighttexture.png",
            "assets/scenes/prefabs/gas_station/gas_station/normaltexture.png",
            "assets/scenes/prefabs/gas_station/gas_station/splattexture0.png",
            "assets/scenes/prefabs/gas_station/gas_station/splattexture1.png",
            "assets/scenes/prefabs/gas_station/gas_station/topologytexture.png",
            "assets/scenes/prefabs/harbor/harbor_1/alphatexture.png",
            "assets/scenes/prefabs/harbor/harbor_1/blendtexture.png",
            "assets/scenes/prefabs/harbor/harbor_1/heighttexture.png",
            "assets/scenes/prefabs/harbor/harbor_1/normaltexture.png",
            "assets/scenes/prefabs/harbor/harbor_1/splattexture0.png",
            "assets/scenes/prefabs/harbor/harbor_1/splattexture1.png",
            "assets/scenes/prefabs/harbor/harbor_1/topologytexture.png",
            "assets/scenes/prefabs/harbor/harbor_2/alphatexture.png",
            "assets/scenes/prefabs/harbor/harbor_2/blendtexture.png",
            "assets/scenes/prefabs/harbor/harbor_2/heighttexture.png",
            "assets/scenes/prefabs/harbor/harbor_2/normaltexture.png",
            "assets/scenes/prefabs/harbor/harbor_2/splattexture0.png",
            "assets/scenes/prefabs/harbor/harbor_2/splattexture1.png",
            "assets/scenes/prefabs/harbor/harbor_2/topologytexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_1/alphatexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_1/biometexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_1/blendtexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_1/heighttexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_1/normaltexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_1/splattexture0.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_1/splattexture1.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_1/topologytexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_1/watertexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_2/alphatexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_2/biometexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_2/blendtexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_2/heighttexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_2/normaltexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_2/splattexture0.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_2/splattexture1.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_2/topologytexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_3/alphatexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_3/biometexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_3/blendtexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_3/heighttexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_3/normaltexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_3/splattexture0.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_3/splattexture1.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_3/topologytexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_3/watertexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_4/alphatexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_4/biometexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_4/blendtexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_4/heighttexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_4/normaltexture.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_4/splattexture0.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_4/splattexture1.png",
            "assets/scenes/prefabs/ice lakes/ice_lake_4/topologytexture.png",
            "assets/scenes/prefabs/junkyard/junkyard/alphatexture.png",
            "assets/scenes/prefabs/junkyard/junkyard/biometexture.png",
            "assets/scenes/prefabs/junkyard/junkyard/blendtexture.png",
            "assets/scenes/prefabs/junkyard/junkyard/heighttexture.png",
            "assets/scenes/prefabs/junkyard/junkyard/normaltexture.png",
            "assets/scenes/prefabs/junkyard/junkyard/splattexture0.png",
            "assets/scenes/prefabs/junkyard/junkyard/splattexture1.png",
            "assets/scenes/prefabs/junkyard/junkyard/topologytexture.png",
            "assets/scenes/prefabs/junkyard/junkyard/watertexture.png",
            "assets/scenes/prefabs/launch site/launchsite/alphatexture.png",
            "assets/scenes/prefabs/launch site/launchsite/biometexture.png",
            "assets/scenes/prefabs/launch site/launchsite/blendtexture.png",
            "assets/scenes/prefabs/launch site/launchsite/heighttexture.png",
            "assets/scenes/prefabs/launch site/launchsite/normaltexture.png",
            "assets/scenes/prefabs/launch site/launchsite/splattexture0.png",
            "assets/scenes/prefabs/launch site/launchsite/splattexture1.png",
            "assets/scenes/prefabs/launch site/launchsite/topologytexture.png",
            "assets/scenes/prefabs/military tunnels/military_tunnel_1/alphatexture.png",
            "assets/scenes/prefabs/military tunnels/military_tunnel_1/blendtexture.png",
            "assets/scenes/prefabs/military tunnels/military_tunnel_1/heighttexture.png",
            "assets/scenes/prefabs/military tunnels/military_tunnel_1/normaltexture.png",
            "assets/scenes/prefabs/military tunnels/military_tunnel_1/splattexture0.png",
            "assets/scenes/prefabs/military tunnels/military_tunnel_1/splattexture1.png",
            "assets/scenes/prefabs/military tunnels/military_tunnel_1/topologytexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_a/alphatexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_a/biometexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_a/blendtexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_a/heighttexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_a/normaltexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_a/splattexture0.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_a/splattexture1.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_a/topologytexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_a/watertexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_b/alphatexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_b/biometexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_b/blendtexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_b/heighttexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_b/normaltexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_b/splattexture0.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_b/splattexture1.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_b/topologytexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_c/alphatexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_c/biometexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_c/blendtexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_c/heighttexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_c/normaltexture.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_c/splattexture0.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_c/splattexture1.png",
            "assets/scenes/prefabs/mining quarries/mining_quarry_c/topologytexture.png",
            "assets/scenes/prefabs/mining/warehouse/alphatexture.png",
            "assets/scenes/prefabs/mining/warehouse/blendtexture.png",
            "assets/scenes/prefabs/mining/warehouse/heighttexture.png",
            "assets/scenes/prefabs/mining/warehouse/normaltexture.png",
            "assets/scenes/prefabs/mining/warehouse/splattexture0.png",
            "assets/scenes/prefabs/mining/warehouse/splattexture1.png",
            "assets/scenes/prefabs/mining/warehouse/topologytexture.png",
            "assets/scenes/prefabs/mountain/mountain_1/alphatexture.png",
            "assets/scenes/prefabs/mountain/mountain_1/biometexture.png",
            "assets/scenes/prefabs/mountain/mountain_1/heighttexture.png",
            "assets/scenes/prefabs/mountain/mountain_1/normaltexture.png",
            "assets/scenes/prefabs/mountain/mountain_1/splattexture0.png",
            "assets/scenes/prefabs/mountain/mountain_1/splattexture1.png",
            "assets/scenes/prefabs/mountain/mountain_1/topologytexture.png",
            "assets/scenes/prefabs/mountain/mountain_2/alphatexture.png",
            "assets/scenes/prefabs/mountain/mountain_2/biometexture.png",
            "assets/scenes/prefabs/mountain/mountain_2/heighttexture.png",
            "assets/scenes/prefabs/mountain/mountain_2/normaltexture.png",
            "assets/scenes/prefabs/mountain/mountain_2/splattexture0.png",
            "assets/scenes/prefabs/mountain/mountain_2/splattexture1.png",
            "assets/scenes/prefabs/mountain/mountain_2/topologytexture.png",
            "assets/scenes/prefabs/mountain/mountain_3/alphatexture.png",
            "assets/scenes/prefabs/mountain/mountain_3/biometexture.png",
            "assets/scenes/prefabs/mountain/mountain_3/heighttexture.png",
            "assets/scenes/prefabs/mountain/mountain_3/normaltexture.png",
            "assets/scenes/prefabs/mountain/mountain_3/splattexture0.png",
            "assets/scenes/prefabs/mountain/mountain_3/splattexture1.png",
            "assets/scenes/prefabs/mountain/mountain_3/topologytexture.png",
            "assets/scenes/prefabs/mountain/mountain_4/alphatexture.png",
            "assets/scenes/prefabs/mountain/mountain_4/biometexture.png",
            "assets/scenes/prefabs/mountain/mountain_4/heighttexture.png",
            "assets/scenes/prefabs/mountain/mountain_4/normaltexture.png",
            "assets/scenes/prefabs/mountain/mountain_4/splattexture0.png",
            "assets/scenes/prefabs/mountain/mountain_4/splattexture1.png",
            "assets/scenes/prefabs/mountain/mountain_4/topologytexture.png",
            "assets/scenes/prefabs/mountain/mountain_5/alphatexture.png",
            "assets/scenes/prefabs/mountain/mountain_5/biometexture.png",
            "assets/scenes/prefabs/mountain/mountain_5/heighttexture.png",
            "assets/scenes/prefabs/mountain/mountain_5/normaltexture.png",
            "assets/scenes/prefabs/mountain/mountain_5/splattexture0.png",
            "assets/scenes/prefabs/mountain/mountain_5/splattexture1.png",
            "assets/scenes/prefabs/mountain/mountain_5/topologytexture.png",
            "assets/scenes/prefabs/overgrowth/overgrowth_dressing/alphatexture.png",
            "assets/scenes/prefabs/overgrowth/overgrowth_dressing/heighttexture.png",
            "assets/scenes/prefabs/overgrowth/overgrowth_dressing/normaltexture.png",
            "assets/scenes/prefabs/overgrowth/overgrowth_dressing/splattexture0.png",
            "assets/scenes/prefabs/overgrowth/overgrowth_dressing/splattexture1.png",
            "assets/scenes/prefabs/overgrowth/overgrowth_dressing/topologytexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_1/alphatexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_1/blendtexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_1/heighttexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_1/normaltexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_1/splattexture0.png",
            "assets/scenes/prefabs/power substations/power_sub_big_1/splattexture1.png",
            "assets/scenes/prefabs/power substations/power_sub_big_1/topologytexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_2/alphatexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_2/biometexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_2/blendtexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_2/heighttexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_2/normaltexture.png",
            "assets/scenes/prefabs/power substations/power_sub_big_2/splattexture0.png",
            "assets/scenes/prefabs/power substations/power_sub_big_2/splattexture1.png",
            "assets/scenes/prefabs/power substations/power_sub_big_2/topologytexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_1/alphatexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_1/biometexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_1/blendtexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_1/heighttexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_1/normaltexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_1/splattexture0.png",
            "assets/scenes/prefabs/power substations/power_sub_small_1/splattexture1.png",
            "assets/scenes/prefabs/power substations/power_sub_small_1/topologytexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_2/alphatexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_2/biometexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_2/blendtexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_2/heighttexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_2/normaltexture.png",
            "assets/scenes/prefabs/power substations/power_sub_small_2/splattexture0.png",
            "assets/scenes/prefabs/power substations/power_sub_small_2/splattexture1.png",
            "assets/scenes/prefabs/power substations/power_sub_small_2/topologytexture.png",
            "assets/scenes/prefabs/powerplant/powerplant/alphatexture.png",
            "assets/scenes/prefabs/powerplant/powerplant/blendtexture.png",
            "assets/scenes/prefabs/powerplant/powerplant/heighttexture.png",
            "assets/scenes/prefabs/powerplant/powerplant/normaltexture.png",
            "assets/scenes/prefabs/powerplant/powerplant/splattexture0.png",
            "assets/scenes/prefabs/powerplant/powerplant/splattexture1.png",
            "assets/scenes/prefabs/powerplant/powerplant/topologytexture.png",
            "assets/scenes/prefabs/production/satellite_dish/alphatexture.png",
            "assets/scenes/prefabs/production/satellite_dish/blendtexture.png",
            "assets/scenes/prefabs/production/satellite_dish/heighttexture.png",
            "assets/scenes/prefabs/production/satellite_dish/normaltexture.png",
            "assets/scenes/prefabs/production/satellite_dish/splattexture0.png",
            "assets/scenes/prefabs/production/satellite_dish/splattexture1.png",
            "assets/scenes/prefabs/production/satellite_dish/topologytexture.png",
            "assets/scenes/prefabs/production/sphere_tank/alphatexture.png",
            "assets/scenes/prefabs/production/sphere_tank/blendtexture.png",
            "assets/scenes/prefabs/production/sphere_tank/heighttexture.png",
            "assets/scenes/prefabs/production/sphere_tank/normaltexture.png",
            "assets/scenes/prefabs/production/sphere_tank/splattexture0.png",
            "assets/scenes/prefabs/production/sphere_tank/splattexture1.png",
            "assets/scenes/prefabs/production/sphere_tank/topologytexture.png",
            "assets/scenes/prefabs/radtown/radtown_small_3/alphatexture.png",
            "assets/scenes/prefabs/radtown/radtown_small_3/blendtexture.png",
            "assets/scenes/prefabs/radtown/radtown_small_3/heighttexture.png",
            "assets/scenes/prefabs/radtown/radtown_small_3/normaltexture.png",
            "assets/scenes/prefabs/radtown/radtown_small_3/splattexture0.png",
            "assets/scenes/prefabs/radtown/radtown_small_3/splattexture1.png",
            "assets/scenes/prefabs/radtown/radtown_small_3/topologytexture.png",
            "assets/scenes/prefabs/supermarket/supermarket/alphatexture.png",
            "assets/scenes/prefabs/supermarket/supermarket/biometexture.png",
            "assets/scenes/prefabs/supermarket/supermarket/blendtexture.png",
            "assets/scenes/prefabs/supermarket/supermarket/heighttexture.png",
            "assets/scenes/prefabs/supermarket/supermarket/normaltexture.png",
            "assets/scenes/prefabs/supermarket/supermarket/splattexture0.png",
            "assets/scenes/prefabs/supermarket/supermarket/splattexture1.png",
            "assets/scenes/prefabs/supermarket/supermarket/topologytexture.png",
            "assets/scenes/prefabs/swamps/swamp_a/alphatexture.png",
            "assets/scenes/prefabs/swamps/swamp_a/biometexture.png",
            "assets/scenes/prefabs/swamps/swamp_a/blendtexture.png",
            "assets/scenes/prefabs/swamps/swamp_a/heighttexture.png",
            "assets/scenes/prefabs/swamps/swamp_a/normaltexture.png",
            "assets/scenes/prefabs/swamps/swamp_a/splattexture0.tga",
            "assets/scenes/prefabs/swamps/swamp_a/splattexture1.tga",
            "assets/scenes/prefabs/swamps/swamp_a/swamp_a_splats_biometexture.png",
            "assets/scenes/prefabs/swamps/swamp_a/swamp_a_splats_splat_texture_0.png",
            "assets/scenes/prefabs/swamps/swamp_a/swamp_a_splats_splat_texture_1.png",
            "assets/scenes/prefabs/swamps/swamp_a/topologytexture.png",
            "assets/scenes/prefabs/swamps/swamp_a/watertexture.png",
            "assets/scenes/prefabs/swamps/swamp_b/alphatexture.png",
            "assets/scenes/prefabs/swamps/swamp_b/biometexture.png",
            "assets/scenes/prefabs/swamps/swamp_b/blendtexture.png",
            "assets/scenes/prefabs/swamps/swamp_b/heighttexture.png",
            "assets/scenes/prefabs/swamps/swamp_b/normaltexture.png",
            "assets/scenes/prefabs/swamps/swamp_b/splattexture0.png",
            "assets/scenes/prefabs/swamps/swamp_b/splattexture1.png",
            "assets/scenes/prefabs/swamps/swamp_b/swamp_b_splats_biometexture.png",
            "assets/scenes/prefabs/swamps/swamp_b/swamp_b_splats_splat_texture_0.png",
            "assets/scenes/prefabs/swamps/swamp_b/swamp_b_splats_splat_texture_1.png",
            "assets/scenes/prefabs/swamps/swamp_b/topologytexture.png",
            "assets/scenes/prefabs/swamps/swamp_b/watertexture.png",
            "assets/scenes/prefabs/swamps/swamp_c/alphatexture.png",
            "assets/scenes/prefabs/swamps/swamp_c/biometexture.png",
            "assets/scenes/prefabs/swamps/swamp_c/blendtexture.png",
            "assets/scenes/prefabs/swamps/swamp_c/heighttexture.png",
            "assets/scenes/prefabs/swamps/swamp_c/normaltexture.png",
            "assets/scenes/prefabs/swamps/swamp_c/splattexture0.png",
            "assets/scenes/prefabs/swamps/swamp_c/splattexture1.png",
            "assets/scenes/prefabs/swamps/swamp_c/swamp_c_splats_biometexture.png",
            "assets/scenes/prefabs/swamps/swamp_c/swamp_c_splats_splat_texture_0.png",
            "assets/scenes/prefabs/swamps/swamp_c/swamp_c_splats_splat_texture_1.png",
            "assets/scenes/prefabs/swamps/swamp_c/topologytexture.png",
            "assets/scenes/prefabs/swamps/swamp_c/watertexture.png",
            "assets/scenes/prefabs/trainyard/trainyard/alphatexture.png",
            "assets/scenes/prefabs/trainyard/trainyard/blendtexture.png",
            "assets/scenes/prefabs/trainyard/trainyard/heighttexture.png",
            "assets/scenes/prefabs/trainyard/trainyard/normaltexture.png",
            "assets/scenes/prefabs/trainyard/trainyard/splattexture0.png",
            "assets/scenes/prefabs/trainyard/trainyard/splattexture1.png",
            "assets/scenes/prefabs/trainyard/trainyard/topologytexture.png",
            "assets/scenes/prefabs/water treatment plant/water_treatment_plant/alphatexture.png",
            "assets/scenes/prefabs/water treatment plant/water_treatment_plant/blendtexture.png",
            "assets/scenes/prefabs/water treatment plant/water_treatment_plant/heighttexture.png",
            "assets/scenes/prefabs/water treatment plant/water_treatment_plant/normaltexture.png",
            "assets/scenes/prefabs/water treatment plant/water_treatment_plant/splattexture0.png",
            "assets/scenes/prefabs/water treatment plant/water_treatment_plant/splattexture1.png",
            "assets/scenes/prefabs/water treatment plant/water_treatment_plant/topologytexture.png",
            "assets/scenes/prefabs/water wells/water_well_a/water_well_a/alphatexture.png",
            "assets/scenes/prefabs/water wells/water_well_a/water_well_a/biometexture.png",
            "assets/scenes/prefabs/water wells/water_well_a/water_well_a/blendtexture.png",
            "assets/scenes/prefabs/water wells/water_well_a/water_well_a/heighttexture.png",
            "assets/scenes/prefabs/water wells/water_well_a/water_well_a/normaltexture.png",
            "assets/scenes/prefabs/water wells/water_well_a/water_well_a/splattexture0.png",
            "assets/scenes/prefabs/water wells/water_well_a/water_well_a/splattexture1.png",
            "assets/scenes/prefabs/water wells/water_well_a/water_well_a/topologytexture.png",
            "assets/scenes/prefabs/water wells/water_well_b/water_well_b/alphatexture.png",
            "assets/scenes/prefabs/water wells/water_well_b/water_well_b/biometexture.png",
            "assets/scenes/prefabs/water wells/water_well_b/water_well_b/blendtexture.png",
            "assets/scenes/prefabs/water wells/water_well_b/water_well_b/heighttexture.png",
            "assets/scenes/prefabs/water wells/water_well_b/water_well_b/normaltexture.png",
            "assets/scenes/prefabs/water wells/water_well_b/water_well_b/splattexture0.png",
            "assets/scenes/prefabs/water wells/water_well_b/water_well_b/splattexture1.png",
            "assets/scenes/prefabs/water wells/water_well_b/water_well_b/topologytexture.png",
            "assets/scenes/prefabs/water wells/water_well_b/water_well_b/watertexture.png",
            "assets/scenes/prefabs/water wells/water_well_c/water_well_c/alphatexture.png",
            "assets/scenes/prefabs/water wells/water_well_c/water_well_c/biometexture.png",
            "assets/scenes/prefabs/water wells/water_well_c/water_well_c/blendtexture.png",
            "assets/scenes/prefabs/water wells/water_well_c/water_well_c/heighttexture.png",
            "assets/scenes/prefabs/water wells/water_well_c/water_well_c/normaltexture.png",
            "assets/scenes/prefabs/water wells/water_well_c/water_well_c/splattexture0.png",
            "assets/scenes/prefabs/water wells/water_well_c/water_well_c/splattexture1.png",
            "assets/scenes/prefabs/water wells/water_well_c/water_well_c/topologytexture.png",
            "assets/scenes/prefabs/water wells/water_well_c/water_well_c/watertexture.png",
            "assets/scenes/prefabs/water wells/water_well_d/water_well_d/alphatexture.png",
            "assets/scenes/prefabs/water wells/water_well_d/water_well_d/biometexture.png",
            "assets/scenes/prefabs/water wells/water_well_d/water_well_d/blendtexture.png",
            "assets/scenes/prefabs/water wells/water_well_d/water_well_d/heighttexture.png",
            "assets/scenes/prefabs/water wells/water_well_d/water_well_d/normaltexture.png",
            "assets/scenes/prefabs/water wells/water_well_d/water_well_d/splattexture0.png",
            "assets/scenes/prefabs/water wells/water_well_d/water_well_d/splattexture1.png",
            "assets/scenes/prefabs/water wells/water_well_d/water_well_d/topologytexture.png",
            "assets/scenes/prefabs/water wells/water_well_d/water_well_d/watertexture.png",
            "assets/scenes/prefabs/water wells/water_well_e/water_well_e/alphatexture.png",
            "assets/scenes/prefabs/water wells/water_well_e/water_well_e/biometexture.png",
            "assets/scenes/prefabs/water wells/water_well_e/water_well_e/blendtexture.png",
            "assets/scenes/prefabs/water wells/water_well_e/water_well_e/heighttexture.png",
            "assets/scenes/prefabs/water wells/water_well_e/water_well_e/normaltexture.png",
            "assets/scenes/prefabs/water wells/water_well_e/water_well_e/splattexture0.png",
            "assets/scenes/prefabs/water wells/water_well_e/water_well_e/splattexture1.png",
            "assets/scenes/prefabs/water wells/water_well_e/water_well_e/topologytexture.png",
            "assets/scenes/prefabs/water wells/water_well_e/water_well_e/watertexture.png",
            "assets/scenes/release/craggyisland/alphatexture.png",
            "assets/scenes/release/craggyisland/biometexture.png",
            "assets/scenes/release/craggyisland/distancetexture.png",
            "assets/scenes/release/craggyisland/heighttexture.png",
            "assets/scenes/release/craggyisland/normaltexture.png",
            "assets/scenes/release/craggyisland/splattexture0.png",
            "assets/scenes/release/craggyisland/splattexture1.png",
            "assets/scenes/release/craggyisland/topologytexture.png",
            "assets/scenes/release/craggyisland/watertexture.png",
            "assets/scenes/release/hapisislandterrain/_normal.png",
            "assets/scenes/release/hapisislandterrain/_normal_cleaned.png",
            "assets/scenes/release/hapisislandterrain/alphatexture.png",
            "assets/scenes/release/hapisislandterrain/biometexture.png",
            "assets/scenes/release/hapisislandterrain/heighttexture.png",
            "assets/scenes/release/hapisislandterrain/legacyterrain_normal.png",
            "assets/scenes/release/hapisislandterrain/normaltexture.png",
            "assets/scenes/release/hapisislandterrain/splattexture0.png",
            "assets/scenes/release/hapisislandterrain/splattexture1.png",
            "assets/scenes/release/hapisislandterrain/topologytexture.png",
            "assets/scenes/release/hapisislandterrain/watertexture.png",
            "assets/scenes/release/savasisland/alphatexture.png",
            "assets/scenes/release/savasisland/biometexture.png",
            "assets/scenes/release/savasisland/heighttexture.png",
            "assets/scenes/release/savasisland/normaltexture.png",
            "assets/scenes/release/savasisland/splattexture0.png",
            "assets/scenes/release/savasisland/splattexture1.png",
            "assets/scenes/release/savasisland/topologytexture.png",
            "assets/scenes/release/savasisland/watertexture.png",
            "assets/scenes/test/bradleylaunchtest/alphatexture.png",
            "assets/scenes/test/bradleylaunchtest/biometexture.png",
            "assets/scenes/test/bradleylaunchtest/heighttexture.png",
            "assets/scenes/test/bradleylaunchtest/normaltexture.png",
            "assets/scenes/test/bradleylaunchtest/splattexture0.png",
            "assets/scenes/test/bradleylaunchtest/splattexture1.png",
            "assets/scenes/test/bradleylaunchtest/topologytexture.png",
            "assets/scenes/test/bradleylaunchtest/watertexture.png",
            "assets/scenes/test/playgroundterrain/alphatexture.png",
            "assets/scenes/test/playgroundterrain/biometexture.png",
            "assets/scenes/test/playgroundterrain/heighttexture.png",
            "assets/scenes/test/playgroundterrain/normaltexture.png",
            "assets/scenes/test/playgroundterrain/splattexture0.png",
            "assets/scenes/test/playgroundterrain/splattexture1.png",
            "assets/scenes/test/playgroundterrain/topologytexture.png",
            "assets/scenes/test/playgroundterrain/watertexture.png",
            "assets/scenes/test/stealthbox/alphatexture.png",
            "assets/scenes/test/stealthbox/biometexture.png",
            "assets/scenes/test/stealthbox/heighttexture.png",
            "assets/scenes/test/stealthbox/normaltexture.png",
            "assets/scenes/test/stealthbox/splattexture0.png",
            "assets/scenes/test/stealthbox/splattexture1.png",
            "assets/scenes/test/stealthbox/topologytexture.png",
            "assets/scenes/test/testlevelterrain/alphatexture.png",
            "assets/scenes/test/testlevelterrain/biometexture.png",
            "assets/scenes/test/testlevelterrain/heighttexture.png",
            "assets/scenes/test/testlevelterrain/normaltexture.png",
            "assets/scenes/test/testlevelterrain/splattexture0.png",
            "assets/scenes/test/testlevelterrain/splattexture1.png",
            "assets/scenes/test/testlevelterrain/topologytexture.png",
            "assets/scenes/test/testlevelterrain/watertexture.png",
            "assets/scenes/test/waterlevelterrain/alphatexture.png",
            "assets/scenes/test/waterlevelterrain/biometexture.png",
            "assets/scenes/test/waterlevelterrain/heighttexture.png",
            "assets/scenes/test/waterlevelterrain/normaltexture.png",
            "assets/scenes/test/waterlevelterrain/splattexture0.png",
            "assets/scenes/test/waterlevelterrain/splattexture1.png",
            "assets/scenes/test/waterlevelterrain/topologytexture.png",
            "assets/scenes/test/waterlevelterrain/watertexture.png",
        };
        #endregion

        #region Materials
        List<string> Materials = new List<string>
        {
            "assets/content/image effects/darkclamp/darknessclamp.mat",
            "assets/content/image effects/linear fog/linearfog.mat",
            "assets/content/image effects/scope/defaultscope.mat",
            "assets/content/materials/cable.mat",
            "assets/content/materials/collider_mesh.mat",
            "assets/content/materials/collider_mesh_convex.mat",
            "assets/content/materials/collider_trigger.mat",
            "assets/content/materials/deployable/deployable_acceptable.mat",
            "assets/content/materials/deployable/deployable_denied.mat",
            "assets/content/materials/displacement/foliagedisplace_circle.mat",
            "assets/content/materials/displacement/foliagedisplace_square.mat",
            "assets/content/materials/gradient.mat",
            "assets/content/materials/ground.mat",
            "assets/content/materials/guide_bad.mat",
            "assets/content/materials/guide_good.mat",
            "assets/content/materials/guide_highlight.mat",
            "assets/content/materials/guide_neutral.mat",
            "assets/content/materials/itemmaterial.mat",
            "assets/content/materials/male.material.mat",
            "assets/content/shaders/gl/gl opaque.mat",
            "assets/content/shaders/gl/gl transparent.mat",
            "assets/content/ui/binocular_overlay.mat",
            "assets/content/ui/goggle_overlay.mat",
            "assets/content/ui/helmet_slit_overlay.mat",
            "assets/content/ui/ingame/compass/compassstrip.mat",
            "assets/content/ui/namefontmaterial.mat",
            "assets/content/ui/playerpreviewglow.mat",
            "assets/content/ui/playerpreviewremovesegments.mat",
            "assets/content/ui/playerpreviewsegments.mat",
            "assets/content/ui/scope_1.mat",
            "assets/content/ui/scope_2.mat",
            "assets/content/ui/ui.maskclear.mat",
            "assets/content/ui/uibackgroundblur-ingamemenu.mat",
            "assets/content/ui/uibackgroundblur-notice.mat",
            "assets/content/ui/uibackgroundblur.mat",
            "assets/icons/fogofwar.mat",
            "assets/icons/greyout.mat",
            "assets/icons/iconmaterial.mat",
        };
        #endregion

        #region Fonts

        public List<string> Fonts = new List<string>
        {
            "assets/content/ui/fonts/droidsansmono.ttf",
            "assets/content/ui/fonts/permanentmarker.ttf",
            "assets/content/ui/fonts/robotocondensed-bold.ttf",
            "assets/content/ui/fonts/robotocondensed-regular.ttf",
        };

        #endregion

        #region HexList

        public List<string> HexList = new List<string>
        {
            "#00FFFF",
            "#000000",
            "#0000FF",
            "#FF00FF",
            "#808080",
            "#008000",
            "#00FF00",
            "#800000",
            "#000080",
            "#808000",
            "#800080",
            "#FF0000",
            "#C0C0C0",
            "#008080",
            "#FFFFFF",
            "#FFFF00",
        };

        #endregion

        #region EffectList
        public HashSet<string> EffectRustList = new HashSet<string>
        {
            "assets/bundled/prefabs/fx/animals/flies/flies_large.prefab",
            "assets/bundled/prefabs/fx/animals/flies/flies_looping.prefab",
            "assets/bundled/prefabs/fx/animals/flies/flies_medium.prefab",
            "assets/bundled/prefabs/fx/animals/flies/flies_small.prefab",
            "assets/bundled/prefabs/fx/beartrap/arm.prefab",
            "assets/bundled/prefabs/fx/beartrap/fire.prefab",
            "assets/bundled/prefabs/fx/bucket_drop_debris.prefab",
            "assets/bundled/prefabs/fx/build/frame_place.prefab",
            "assets/bundled/prefabs/fx/build/promote_metal.prefab",
            "assets/bundled/prefabs/fx/build/promote_stone.prefab",
            "assets/bundled/prefabs/fx/build/promote_toptier.prefab",
            "assets/bundled/prefabs/fx/build/promote_wood.prefab",
            "assets/bundled/prefabs/fx/build/repair.prefab",
            "assets/bundled/prefabs/fx/build/repair_failed.prefab",
            "assets/bundled/prefabs/fx/build/repair_full.prefab",
            "assets/bundled/prefabs/fx/building/fort_metal_gib.prefab",
            "assets/bundled/prefabs/fx/building/metal_sheet_gib.prefab",
            "assets/bundled/prefabs/fx/building/stone_gib.prefab",
            "assets/bundled/prefabs/fx/building/thatch_gib.prefab",
            "assets/bundled/prefabs/fx/building/wood_gib.prefab",
            "assets/bundled/prefabs/fx/collect/collect fuel barrel.prefab",
            "assets/bundled/prefabs/fx/collect/collect mushroom.prefab",
            "assets/bundled/prefabs/fx/collect/collect plant.prefab",
            "assets/bundled/prefabs/fx/collect/collect stone.prefab",
            "assets/bundled/prefabs/fx/collect/collect stump.prefab",
            "assets/bundled/prefabs/fx/decals/blood/decal_blood_splatter_01.prefab",
            "assets/bundled/prefabs/fx/decals/blood/decal_blood_splatter_02.prefab",
            "assets/bundled/prefabs/fx/decals/blood/decal_blood_splatter_03.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/dirt/decal_blunt_dirt_01.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/forest/decal_blunt_forest_01.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/glass/decal_blunt_glass.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/grass/decal_blunt_grass_01.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/metalore/decal_blunt_metalore_01.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/path/decal_blunt_path_01.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/rock/decal_blunt_rock_01.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/sand/decal_blunt_sand_01.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/snow/decal_blunt_snow_01.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/tundra/decal_blunt_tundra_01.prefab",
            "assets/bundled/prefabs/fx/decals/blunt/wood/wood 1.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/concrete/decal_bullet_concrete_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/concrete/decal_bullet_concrete_02.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/concrete/decal_bullet_concrete_03.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/concrete/decal_bullet_concrete_04.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/dirt/decal_bullet_dirt_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/flesh/decal_bullet_flesh_entry_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/flesh/decal_bullet_flesh_exit_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/forest/decal_bullet_forest_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/glass/decal_bullet_glass.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/glass/decal_bullet_glass2.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/glass/decal_bullet_glass3.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/grass/decal_bullet_grass_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/metal/decal_bullet_metal_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/metal/decal_bullet_metal_02.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/metal/decal_bullet_metal_03.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/metalore/decal_bullet_metalore_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/path/decal_bullet_road_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/rock/decal_bullet_rock.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/sand/decal_bullet_sand_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/sandbag/decal_bullet_sandbag_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/snow/decal_bullet_snow_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/tundra/decal_bullet_tundra.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/wood/decal_bullet_wood_01.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/wood/decal_bullet_wood_02.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/wood/decal_bullet_wood_03.prefab",
            "assets/bundled/prefabs/fx/decals/bullet/wood/decal_bullet_wood_04.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/dirt/decal_footprint_human_bare_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/dirt/decal_footprint_human_bare_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/forest/decal_footprint_human_bare_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/forest/decal_footprint_human_bare_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/grass/decal_footprint_human_bare_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/grass/decal_footprint_human_bare_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/sand/decal_footprint_human_sand_bare_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/sand/decal_footprint_human_sand_bare_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/snow/decal_footprint_human_bare_snow_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/snow/decal_footprint_human_snow_bare_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/tundra/decal_footprint_human_bare_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/bare/tundra/decal_footprint_human_bare_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/dirt/decal_footprint_human_boot_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/dirt/decal_footprint_human_boot_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/forest/decal_footprint_human_boot_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/forest/decal_footprint_human_boot_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/grass/decal_footprint_human_boot_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/grass/decal_footprint_human_boot_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/sand/decal_footprint_human_sand_boot_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/sand/decal_footprint_human_sand_boot_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/snow/decal_footprint_human_snow_boot_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/snow/decal_footprint_human_snow_boot_right.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/tundra/decal_footprint_human_boot_left.prefab",
            "assets/bundled/prefabs/fx/decals/footstep/boot/tundra/decal_footprint_human_boot_right.prefab",
            "assets/bundled/prefabs/fx/decals/slash/dirt/decal_slash_dirt_01.prefab",
            "assets/bundled/prefabs/fx/decals/slash/forest/decal_slash_forest_01.prefab",
            "assets/bundled/prefabs/fx/decals/slash/glass/decal_slash_glass.prefab",
            "assets/bundled/prefabs/fx/decals/slash/grass/decal_slash_grass_01.prefab",
            "assets/bundled/prefabs/fx/decals/slash/metalore/decal_slash_metalore_01.prefab",
            "assets/bundled/prefabs/fx/decals/slash/path/decal_slash_path_01.prefab",
            "assets/bundled/prefabs/fx/decals/slash/rock/decal_slash_rock_01.prefab",
            "assets/bundled/prefabs/fx/decals/slash/sand/decal_slash_sand_01.prefab",
            "assets/bundled/prefabs/fx/decals/slash/snow/decal_slash_snow_01.prefab",
            "assets/bundled/prefabs/fx/decals/slash/tundra/decal_slash_tundra_01.prefab",
            "assets/bundled/prefabs/fx/decals/slash/wood/wood 1.prefab",
            "assets/bundled/prefabs/fx/decals/stab/concrete/decal_stab_concrete_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/concrete/decal_stab_concrete_02.prefab",
            "assets/bundled/prefabs/fx/decals/stab/concrete/decal_stab_concrete_03.prefab",
            "assets/bundled/prefabs/fx/decals/stab/concrete/decal_stab_concrete_04.prefab",
            "assets/bundled/prefabs/fx/decals/stab/dirt/decal_stab_dirt_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/flesh/decal_stab_flesh_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/forest/decal_stab_forest_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/glass/decal_stab_glass.prefab",
            "assets/bundled/prefabs/fx/decals/stab/grass/decal_stab_grass_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/metalore/decal_stab_metalore_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/path/decal_stab_path_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/rock/decal_stab_rock_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/sand/decal_stab_sand_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/snow/decal_stab_snow_01.prefab",
            "assets/bundled/prefabs/fx/decals/stab/tundra/decal_stab_snow_01.prefab",
            "assets/bundled/prefabs/fx/dig_effect.prefab",
            "assets/bundled/prefabs/fx/displacement/player/grass/player_displacement.prefab",
            "assets/bundled/prefabs/fx/door/barricade_spawn.prefab",
            "assets/bundled/prefabs/fx/entities/loot_barrel/gib.prefab",
            "assets/bundled/prefabs/fx/entities/loot_barrel/impact.prefab",
            "assets/bundled/prefabs/fx/entities/pumpkin/gib.prefab",
            "assets/bundled/prefabs/fx/entities/tree/tree-impact.prefab",
            "assets/bundled/prefabs/fx/explosions/explosion_01.prefab",
            "assets/bundled/prefabs/fx/explosions/explosion_02.prefab",
            "assets/bundled/prefabs/fx/explosions/explosion_03.prefab",
            "assets/bundled/prefabs/fx/explosions/explosion_core.prefab",
            "assets/bundled/prefabs/fx/explosions/explosion_core_flash.prefab",
            "assets/bundled/prefabs/fx/explosions/water_bomb.prefab",
            "assets/bundled/prefabs/fx/fire/fire_v2.prefab",
            "assets/bundled/prefabs/fx/fire/fire_v3.prefab",
            "assets/bundled/prefabs/fx/fire/oilbarrel-fire.prefab",
            "assets/bundled/prefabs/fx/fire_explosion.prefab",
            "assets/bundled/prefabs/fx/firebomb.prefab",
            "assets/bundled/prefabs/fx/gas_explosion_small.prefab",
            "assets/bundled/prefabs/fx/gestures/cameratakescreenshot.prefab",
            "assets/bundled/prefabs/fx/gestures/cut_meat.prefab",
            "assets/bundled/prefabs/fx/gestures/drink_generic.prefab",
            "assets/bundled/prefabs/fx/gestures/drink_vomit.prefab",
            "assets/bundled/prefabs/fx/gestures/eat_candybar.prefab",
            "assets/bundled/prefabs/fx/gestures/eat_celery.prefab",
            "assets/bundled/prefabs/fx/gestures/eat_chewy_meat.prefab",
            "assets/bundled/prefabs/fx/gestures/eat_chips.prefab",
            "assets/bundled/prefabs/fx/gestures/eat_generic.prefab",
            "assets/bundled/prefabs/fx/gestures/eat_soft.prefab",
            "assets/bundled/prefabs/fx/gestures/lick.prefab",
            "assets/bundled/prefabs/fx/gestures/take_pills.prefab",
            "assets/bundled/prefabs/fx/headshot.prefab",
            "assets/bundled/prefabs/fx/headshot_2d.prefab",
            "assets/bundled/prefabs/fx/hit_notify.prefab",
            "assets/bundled/prefabs/fx/impacts/additive/explosion.prefab",
            "assets/bundled/prefabs/fx/impacts/additive/fire.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/cloth/cloth1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/clothflesh/clothflesh1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/concrete/concrete1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/dirt/dirt1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/flesh/fleshbloodimpact.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/generic/generic1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/glass/glass1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/grass/grass1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/gravel/slash_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/metal/metal1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/metalore/slash_metalore_01.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/rock/slash_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/sand/sand.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/snow/snow.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/stones/slash_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/water/water.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/wood/wood1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/cloth/cloth1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/clothflesh/clothflesh1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/concrete/concrete1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/dirt/dirt1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/flesh/fleshbloodimpact.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/forest/forest1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/generic/generic1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/generic/generic2.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/generic/generic3.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/generic/generic4.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/glass/glass1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/grass/grass1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/gravel/bullet_impact_rock.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/metal/metal1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/metalore/bullet_impact_metalore.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/path/path1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/rock/bullet_impact_rock.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/sand/sand1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/sandbag/sand_bag_impact.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/snow/snow1.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/stones/bullet_impact_rock.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/tundra/bullet_impact_tundra.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/water/water.prefab",
            "assets/bundled/prefabs/fx/impacts/bullet/wood/wood1.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/cloth/footstep-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/concrete/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/dirt/footstep-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/forest/footstep-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/generic/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/grass/footstep-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/gravel/footstep-gravel.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/metal/footstep-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/metalore/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/rock/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/sand/footstep-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/snow/human_footstep_snow.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/stones/footstep-stones.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/water/boot_footstep_water.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/barefoot/wood/footstep-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/cloth/footstep-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/concrete/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/dirt/footstep-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/forest/footstep-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/generic/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/grass/footstep-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/gravel/footstep-gravel.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/metal/footstep-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/metalore/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/rock/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/sand/footstep-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/snow/human_footstep_snow.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/stones/footstep-stones.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/water/boot_footstep_water.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/boots/wood/footstep-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/cloth/footstep-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/concrete/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/dirt/footstep-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/forest/footstep-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/generic/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/grass/footstep-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/gravel/footstep-gravel.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/metal/footstep-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/metalore/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/rock/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/sand/footstep-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/snow/human_footstep_snow.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/stones/footstep-stones.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/water/boot_footstep_water.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/burlap/wood/footstep-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/crouch/water/boot_footstep_water.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/cloth/footstep-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/concrete/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/dirt/footstep-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/forest/footstep-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/generic/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/grass/footstep-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/gravel/footstep-gravel.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/metal/footstep-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/metalore/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/rock/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/sand/footstep-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/snow/human_footstep_snow.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/stones/footstep-stones.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/water/boot_footstep_water.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/hide/wood/footstep-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/cloth/footstep-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/concrete/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/dirt/footstep-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/forest/footstep-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/generic/footstep-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/grass/footstep-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/gravel/footstep-gravel.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/metal/footstep-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/metalore/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/rock/footstep-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/sand/footstep-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/snow/human_footstep_snow.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/stones/footstep-stones.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/water/boot_footstep_water.prefab",
            "assets/bundled/prefabs/fx/impacts/footstep/rubberboots/wood/footstep-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/gravel/jump-land-gravel.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/stones/jump-land-stones.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/gravel/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/stones/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/boots/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/gravel/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/stones/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/burlap/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/gravel/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/stones/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/hide/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/gravel/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/stones/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/rubberboots/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/gravel/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/stones/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/barefoot/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/gravel/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/stones/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/boots/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/gravel/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/stones/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/burlap/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/gravel/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/stones/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/hide/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/gravel/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/stones/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/water/jump_land_water.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-start/rubberboots/wood/jump-land-wood.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-meat-hard.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-meat-med.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-meat-soft.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-can-hard.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-can-med.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-can-soft.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-hollow-hard.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-hollow-med.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-hollow-soft.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-medium-rattley-hard.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-medium-rattley-med.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-medium-rattley-soft.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-thin-hollow-hard.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-thin-hollow-med.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-metal-thin-hollow-soft.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-stone-hard.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-stone-med.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-stone-soft.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-tool-hard.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-tool-med.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-tool-soft.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-wood-hard.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-wood-med.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-wood-small-hard.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-wood-small-med.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-wood-small-soft.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/phys-impact-wood-soft.prefab",
            "assets/bundled/prefabs/fx/impacts/physics/water-enter-exit.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/blood12slash_1.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/blood12slash_2.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/blood13slash.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/blood14slash.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/cloth/cloth1.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/clothflesh/clothflesh1.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/concrete/slash_concrete_01.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/dirt/dirt1.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/flesh/fleshbloodimpact.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/generic/generic1.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/glass/glass1.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/grass/grass1.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/grass/grass2.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/gravel/slash_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/metal/metal1.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/metal/metal2.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/metalore/slash_metalore_01.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/rock/slash_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/sand/sand.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/snow/snow.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/stones/slash_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/water/water.prefab",
            "assets/bundled/prefabs/fx/impacts/slash/wood/wood1.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/cloth/cloth1.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/clothflesh/clothflesh1.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/dirt/dirt1.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/flesh/fleshbloodimpact.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/generic/generic1.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/glass/glass1.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/grass/grass1.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/gravel/stab_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/metal/metal1.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/metalore/slash_metalore_01.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/rock/stab_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/sand/sand.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/snow/snow.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/stones/stab_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/water/water.prefab",
            "assets/bundled/prefabs/fx/impacts/stab/wood/wood1.prefab",
            "assets/bundled/prefabs/fx/invite_notice.prefab",
            "assets/bundled/prefabs/fx/item_break.prefab",
            "assets/bundled/prefabs/fx/missing.prefab",
            "assets/bundled/prefabs/fx/notice/item.select.fx.prefab",
            "assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab",
            "assets/bundled/prefabs/fx/notice/loot.drag.dropsuccess.fx.prefab",
            "assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab",
            "assets/bundled/prefabs/fx/notice/loot.drag.itemdrop.fx.prefab",
            "assets/bundled/prefabs/fx/notice/loot.start.fx.prefab",
            "assets/bundled/prefabs/fx/notice/stack.ui.fx.prefab",
            "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab",
            "assets/bundled/prefabs/fx/oil_geyser.prefab",
            "assets/bundled/prefabs/fx/oiljack/pump_down.prefab",
            "assets/bundled/prefabs/fx/oiljack/pump_up.prefab",
            "assets/bundled/prefabs/fx/ore_break.prefab",
            "assets/bundled/prefabs/fx/player/beartrap_blood.prefab",
            "assets/bundled/prefabs/fx/player/beartrap_clothing_rustle.prefab",
            "assets/bundled/prefabs/fx/player/beartrap_scream.prefab",
            "assets/bundled/prefabs/fx/player/bloodspurt_wounded_head.prefab",
            "assets/bundled/prefabs/fx/player/bloodspurt_wounded_leftarm.prefab",
            "assets/bundled/prefabs/fx/player/bloodspurt_wounded_pelvis.prefab",
            "assets/bundled/prefabs/fx/player/bloodspurt_wounded_stomache.prefab",
            "assets/bundled/prefabs/fx/player/debugeffect.prefab",
            "assets/bundled/prefabs/fx/player/drown.prefab",
            "assets/bundled/prefabs/fx/player/fall-damage.prefab",
            "assets/bundled/prefabs/fx/player/flinch.prefab",
            "assets/bundled/prefabs/fx/player/frosty_breath.prefab",
            "assets/bundled/prefabs/fx/player/groundfall.prefab",
            "assets/bundled/prefabs/fx/player/gutshot_scream.prefab",
            "assets/bundled/prefabs/fx/player/howl.prefab",
            "assets/bundled/prefabs/fx/player/onfire.prefab",
            "assets/bundled/prefabs/fx/player/swing_weapon.prefab",
            "assets/bundled/prefabs/fx/repairbench/itemrepair.prefab",
            "assets/bundled/prefabs/fx/ricochet/ricochet1.prefab",
            "assets/bundled/prefabs/fx/ricochet/ricochet2.prefab",
            "assets/bundled/prefabs/fx/ricochet/ricochet3.prefab",
            "assets/bundled/prefabs/fx/ricochet/ricochet4.prefab",
            "assets/bundled/prefabs/fx/screen_jump.prefab",
            "assets/bundled/prefabs/fx/screen_land.prefab",
            "assets/bundled/prefabs/fx/smoke/generator_smoke.prefab",
            "assets/bundled/prefabs/fx/smoke_cover_full.prefab",
            "assets/bundled/prefabs/fx/smoke_signal.prefab",
            "assets/bundled/prefabs/fx/smoke_signal_full.prefab",
            "assets/bundled/prefabs/fx/survey_explosion.prefab",
            "assets/bundled/prefabs/fx/takedamage_generic.prefab",
            "assets/bundled/prefabs/fx/takedamage_hit.prefab",
            "assets/bundled/prefabs/fx/water/groundsplash.prefab",
            "assets/bundled/prefabs/fx/water/midair_splash.prefab",
            "assets/bundled/prefabs/fx/water/playerjumpinwater.prefab",
            "assets/bundled/prefabs/fx/weapons/landmine/landmine_explosion.prefab",
            "assets/bundled/prefabs/fx/weapons/landmine/landmine_trigger.prefab",
            "assets/bundled/prefabs/fx/weapons/rifle_jingle1.prefab",
            "assets/bundled/prefabs/fx/weapons/rifle_jingle2.prefab",
            "assets/bundled/prefabs/fx/weapons/survey_charge/survey_charge_stick.prefab",
            "assets/bundled/prefabs/fx/well/pump_down.prefab",
            "assets/bundled/prefabs/fx/well/pump_up.prefab",
            "assets/content/effects/candle.prefab",
            "assets/content/effects/electrical/fx-fusebox-sparks.prefab",
            "assets/content/effects/fireworks/pfx fireworks boomer blue v2.prefab",
            "assets/content/effects/fireworks/pfx fireworks boomer golden xl v2.prefab",
            "assets/content/effects/fireworks/pfx fireworks boomer green v2.prefab",
            "assets/content/effects/fireworks/pfx fireworks boomer orange v2.prefab",
            "assets/content/effects/fireworks/pfx fireworks boomer red v2.prefab",
            "assets/content/effects/fireworks/pfx fireworks boomer violet v2.prefab",
            "assets/content/effects/fireworks/pfx fireworks roman candle.prefab",
            "assets/content/effects/fireworks/pfx fireworks volcano red.prefab",
            "assets/content/effects/fireworks/pfx fireworks volcano violet.prefab",
            "assets/content/effects/fireworks/pfx fireworks volcano.prefab",
            "assets/content/effects/fireworks/pfx roman candle projectile blue.prefab",
            "assets/content/effects/fireworks/pfx roman candle projectile green.prefab",
            "assets/content/effects/fireworks/pfx roman candle projectile red.prefab",
            "assets/content/effects/fireworks/pfx roman candle projectile violet.prefab",
            "assets/content/effects/materials/fog/fog_wall.prefab",
            "assets/content/effects/materials/fog/height_fog.prefab",
            "assets/content/effects/mountainfume/mountainfog.prefab",
            "assets/content/effects/mountainfume/mountainfumes.prefab",
            "assets/content/effects/muzzleflashes/muzzleflash_lightex.prefab",
            "assets/content/effects/muzzleflashes/muzzleflash_lightex_large.prefab",
            "assets/content/effects/muzzleflashes/muzzleflash_lightex_tiny.prefab",
            "assets/content/effects/muzzleflashes/other/eoka_attack.prefab",
            "assets/content/effects/muzzleflashes/other/eoka_flint_spark.prefab",
            "assets/content/effects/muzzleflashes/pistol/muzzle_flash_nailgun.prefab",
            "assets/content/effects/muzzleflashes/pistol/muzzle_flash_pistol.prefab",
            "assets/content/effects/muzzleflashes/pistol/muzzle_flash_pistol_braked.prefab",
            "assets/content/effects/muzzleflashes/pistol/muzzle_flash_pistol_large.prefab",
            "assets/content/effects/muzzleflashes/pistol/muzzle_flash_pistol_silenced.prefab",
            "assets/content/effects/muzzleflashes/pistol/shell_eject_pistol.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_big.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_braked.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_braked_l96.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_braked_lr300.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_braked_semi.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_l96.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_quad.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_quad_m39.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_silenced.prefab",
            "assets/content/effects/muzzleflashes/rifle/muzzle_flash_rifle_silenced_highpowered.prefab",
            "assets/content/effects/muzzleflashes/rifle/shell_eject_rifle.prefab",
            "assets/content/effects/muzzleflashes/rifle/shell_eject_rifle_bolt.prefab",
            "assets/content/effects/muzzleflashes/rifle/shell_eject_rifle_m249.prefab",
            "assets/content/effects/muzzleflashes/rifle/shell_eject_rifle_m39.prefab",
            "assets/content/effects/muzzleflashes/shotgun/muzzle_flash_shotgun_braked.prefab",
            "assets/content/effects/muzzleflashes/shotgun/muzzle_flash_shotgun_narrow.prefab",
            "assets/content/effects/muzzleflashes/shotgun/muzzle_flash_shotgun_silenced.prefab",
            "assets/content/effects/muzzleflashes/shotgun/muzzle_flash_shotgun_wide.prefab",
            "assets/content/effects/muzzleflashes/shotgun/shell_eject_shotgun.prefab",
            "assets/content/effects/muzzleflashes/smg/muzzle_flash_smg.prefab",
            "assets/content/effects/muzzleflashes/smg/muzzle_flash_smg_boosted.prefab",
            "assets/content/effects/muzzleflashes/smg/muzzle_flash_smg_braked.prefab",
            "assets/content/effects/muzzleflashes/smg/muzzle_flash_smg_silenced.prefab",
            "assets/content/effects/muzzleflashes/smg/muzzle_flash_smg_twin.prefab",
            "assets/content/effects/muzzleflashes/smg/shell_eject_smg.prefab",
            "assets/content/effects/objects/decal_box.prefab",
            "assets/content/effects/water/bullet_trails_underwater_01.prefab",
            "assets/content/effects/wip/frosty_breath_fx.prefab",
            "assets/content/effects/wip/muzzle_smoke.prefab",
            "assets/content/nature/dunes/pfx sand.prefab",
            "assets/content/nature/ores/highyield_fx.prefab",
            "assets/content/nature/treesprefabs/trees/effects/tree_bonus_effect.prefab",
            "assets/content/nature/treesprefabs/trees/effects/tree_fall.prefab",
            "assets/content/nature/treesprefabs/trees/effects/tree_fall_impact.prefab",
            "assets/content/nature/treesprefabs/trees/effects/tree_impact.prefab",
            "assets/content/nature/treesprefabs/trees/effects/tree_impact_mask.prefab",
            "assets/content/nature/treesprefabs/trees/effects/tree_impact_small.prefab",
            "assets/content/nature/treesprefabs/trees/effects/tree_marking.prefab",
            "assets/content/nature/treesprefabs/trees/effects/tree_marking_nospherecast.prefab",
            "assets/content/nature/treesprefabs/trees/effects/tree_marking_spawn.prefab",
            "assets/content/props/fog machine/effects/cascade_smoke.prefab",
            "assets/content/props/fog machine/effects/emission.prefab",
            "assets/content/props/light_fixtures/v2/radtown work prefabs/bandit swamp fog fx.prefab",
            "assets/content/props/light_fixtures/v2/radtown work prefabs/clone_vat_fx.prefab",
            "assets/content/props/light_fixtures/v2/radtown work prefabs/giant excavator dig fx.prefab",
            "assets/content/props/light_fixtures/v2/radtown work prefabs/giant excavator fx.prefab",
            "assets/content/props/light_fixtures/v2/radtown work prefabs/lighthousefx-ph.prefab",
            "assets/content/props/light_fixtures/v2/radtown work prefabs/oil rig fx.prefab",
            "assets/content/props/light_fixtures/v2/radtown work prefabs/small oil rig fx.prefab",
            "assets/content/structures/excavator/prefabs/effects/enginerumble.prefab",
            "assets/content/structures/excavator/prefabs/effects/rockvibration.prefab",
            "assets/content/vehicles/boats/effects/small-boat-push-land.prefab",
            "assets/content/vehicles/boats/effects/small-boat-push-water.prefab",
            "assets/content/vehicles/boats/effects/splash.prefab",
            "assets/content/vehicles/boats/effects/splashloop.prefab",
            "assets/content/vehicles/minicopter/debris_effect.prefab",
            "assets/content/vehicles/scrap heli carrier/effects/debris_effect.prefab",
            "assets/content/vehicles/scrap heli carrier/effects/wheel-impact.prefab",
            "assets/content/weapons/_gestures/effects/drink.prefab",
            "assets/content/weapons/_gestures/effects/eat_1hand_celery.prefab",
            "assets/content/weapons/_gestures/effects/eat_2hand_chewymeat.prefab",
            "assets/prefabs/ammo/40mmgrenade/effects/40mm_he_explosion.prefab",
            "assets/prefabs/building/door.double.hinged/effects/door-double-metal-close-end.prefab",
            "assets/prefabs/building/door.double.hinged/effects/door-double-metal-close-start.prefab",
            "assets/prefabs/building/door.double.hinged/effects/door-double-metal-open-start.prefab",
            "assets/prefabs/building/door.double.hinged/effects/door-double-wood-close-end.prefab",
            "assets/prefabs/building/door.double.hinged/effects/door-double-wood-close-start.prefab",
            "assets/prefabs/building/door.double.hinged/effects/door-double-wood-open-start.prefab",
            "assets/prefabs/building/door.hinged/effects/door-metal-close-end.prefab",
            "assets/prefabs/building/door.hinged/effects/door-metal-close-start.prefab",
            "assets/prefabs/building/door.hinged/effects/door-metal-deploy.prefab",
            "assets/prefabs/building/door.hinged/effects/door-metal-impact.prefab",
            "assets/prefabs/building/door.hinged/effects/door-metal-knock.prefab",
            "assets/prefabs/building/door.hinged/effects/door-metal-open-end.prefab",
            "assets/prefabs/building/door.hinged/effects/door-metal-open-start.prefab",
            "assets/prefabs/building/door.hinged/effects/door-wood-close-end.prefab",
            "assets/prefabs/building/door.hinged/effects/door-wood-close-start.prefab",
            "assets/prefabs/building/door.hinged/effects/door-wood-deploy.prefab",
            "assets/prefabs/building/door.hinged/effects/door-wood-impact.prefab",
            "assets/prefabs/building/door.hinged/effects/door-wood-knock.prefab",
            "assets/prefabs/building/door.hinged/effects/door-wood-open-end.prefab",
            "assets/prefabs/building/door.hinged/effects/door-wood-open-start.prefab",
            "assets/prefabs/building/door.hinged/effects/gate-external-metal-close-end.prefab",
            "assets/prefabs/building/door.hinged/effects/gate-external-metal-close-start.prefab",
            "assets/prefabs/building/door.hinged/effects/gate-external-metal-open-end.prefab",
            "assets/prefabs/building/door.hinged/effects/gate-external-metal-open-start.prefab",
            "assets/prefabs/building/door.hinged/effects/gate-external-wood-close-end.prefab",
            "assets/prefabs/building/door.hinged/effects/gate-external-wood-close-start.prefab",
            "assets/prefabs/building/door.hinged/effects/gate-external-wood-open-end.prefab",
            "assets/prefabs/building/door.hinged/effects/gate-external-wood-open-start.prefab",
            "assets/prefabs/building/floor.grill/effects/floor-grill-deploy.prefab",
            "assets/prefabs/building/floor.ladder.hatch/effects/door-ladder-hatch-close-end.prefab",
            "assets/prefabs/building/floor.ladder.hatch/effects/door-ladder-hatch-close-start.prefab",
            "assets/prefabs/building/floor.ladder.hatch/effects/door-ladder-hatch-deploy.prefab",
            "assets/prefabs/building/floor.ladder.hatch/effects/door-ladder-hatch-open-start.prefab",
            "assets/prefabs/building/ladder.wall.wood/effects/wood-ladder-deploy.prefab",
            "assets/prefabs/building/wall.external.high.stone/effects/wall-external-stone-deploy.prefab",
            "assets/prefabs/building/wall.external.high.wood/effects/wall-external-wood-deploy.prefab",
            "assets/prefabs/building/wall.frame.cell/effects/cell-wall-door-deploy.prefab",
            "assets/prefabs/building/wall.frame.cell/effects/door-cell-metal-close-end.prefab",
            "assets/prefabs/building/wall.frame.cell/effects/door-cell-metal-close-start.prefab",
            "assets/prefabs/building/wall.frame.cell/effects/door-cell-metal-open-start.prefab",
            "assets/prefabs/building/wall.frame.fence/effects/chain-link-fence-deploy.prefab",
            "assets/prefabs/building/wall.frame.fence/effects/chain-link-impact.prefab",
            "assets/prefabs/building/wall.frame.fence/effects/door-fence-metal-close-end.prefab",
            "assets/prefabs/building/wall.frame.fence/effects/door-fence-metal-close-start.prefab",
            "assets/prefabs/building/wall.frame.fence/effects/door-fence-metal-open-end.prefab",
            "assets/prefabs/building/wall.frame.fence/effects/door-fence-metal-open-start.prefab",
            "assets/prefabs/building/wall.frame.garagedoor/effects/garagedoor.movement.begin.prefab",
            "assets/prefabs/building/wall.frame.garagedoor/effects/garagedoor.movement.finish.close.prefab",
            "assets/prefabs/building/wall.frame.garagedoor/effects/garagedoor.movement.finish.open.prefab",
            "assets/prefabs/building/wall.frame.netting/effects/netting-deploy.prefab",
            "assets/prefabs/building/wall.frame.shopfront/effects/door-small-wood-close-end.prefab",
            "assets/prefabs/building/wall.frame.shopfront/effects/door-small-wood-close-start.prefab",
            "assets/prefabs/building/wall.frame.shopfront/effects/door-small-wood-open-end.prefab",
            "assets/prefabs/building/wall.frame.shopfront/effects/door-small-wood-open-start.prefab",
            "assets/prefabs/building/wall.frame.shopfront/effects/metal_transaction_complete.prefab",
            "assets/prefabs/building/wall.frame.shopfront/effects/shop-front-deploy.prefab",
            "assets/prefabs/building/wall.window.bars/effects/window-bars-metal-deploy.prefab",
            "assets/prefabs/building/wall.window.bars/effects/window-bars-wood-deploy.prefab",
            "assets/prefabs/building/wall.window.embrasure/effects/window-embrasure-deploy.prefab",
            "assets/prefabs/building/wall.window.reinforcedglass/effects/reinforced-glass-window-deploy.prefab",
            "assets/prefabs/building/wall.window.shutter/effects/shutter-wood-deploy.prefab",
            "assets/prefabs/building/wall.window.shutter/effects/shutters-wood-close-end.prefab",
            "assets/prefabs/building/wall.window.shutter/effects/shutters-wood-close-start.prefab",
            "assets/prefabs/building/wall.window.shutter/effects/shutters-wood-open-end.prefab",
            "assets/prefabs/building/wall.window.shutter/effects/shutters-wood-open-start.prefab",
            "assets/prefabs/clothes/diving.tank/effects/exhale_bubbles.prefab",
            "assets/prefabs/clothes/diving.tank/effects/scuba_exhale.prefab",
            "assets/prefabs/clothes/diving.tank/effects/scuba_inhale.prefab",
            "assets/prefabs/clothes/diving.tank/effects/tank_refill.prefab",
            "assets/prefabs/deployable/barricades/effects/barricade-concrete-deploy.prefab",
            "assets/prefabs/deployable/barricades/effects/barricade-metal-deploy.prefab",
            "assets/prefabs/deployable/barricades/effects/barricade-sandbags-deploy.prefab",
            "assets/prefabs/deployable/barricades/effects/barricade-stone-deploy.prefab",
            "assets/prefabs/deployable/barricades/effects/barricade-wood-deploy.prefab",
            "assets/prefabs/deployable/barricades/effects/damage.prefab",
            "assets/prefabs/deployable/bbq/effects/barbeque-deploy.prefab",
            "assets/prefabs/deployable/bear trap/effects/bear-trap-deploy.prefab",
            "assets/prefabs/deployable/bed/effects/bed-deploy.prefab",
            "assets/prefabs/deployable/campfire/effects/campfire-deploy.prefab",
            "assets/prefabs/deployable/ceiling light/effects/ceiling-light-deploy.prefab",
            "assets/prefabs/deployable/chair/effects/chair-deploy.prefab",
            "assets/prefabs/deployable/chinooklockedcrate/effects/landing.prefab",
            "assets/prefabs/deployable/dropbox/effects/dropbox-deploy.prefab",
            "assets/prefabs/deployable/dropbox/effects/submit_items.prefab",
            "assets/prefabs/deployable/floor spikes/effects/floor-spikes-deploy.prefab",
            "assets/prefabs/deployable/fridge/effects/fridge-deploy.prefab",
            "assets/prefabs/deployable/furnace.large/effects/furnace-large-deploy.prefab",
            "assets/prefabs/deployable/furnace/effects/furnace-deploy.prefab",
            "assets/prefabs/deployable/lantern/effects/lantern-deploy.prefab",
            "assets/prefabs/deployable/large wood storage/effects/large-wood-box-deploy.prefab",
            "assets/prefabs/deployable/liquidbarrel/effects/liquid-barrel-deploy.prefab",
            "assets/prefabs/deployable/liquidbarrel/effects/splashloop.prefab",
            "assets/prefabs/deployable/liquidbarrel/effects/taploop.prefab",
            "assets/prefabs/deployable/locker/effects/locker-deploy.prefab",
            "assets/prefabs/deployable/mailbox/effects/mailbox-deploy.prefab",
            "assets/prefabs/deployable/oil refinery/effects/oil-refinery-deploy.prefab",
            "assets/prefabs/deployable/planters/effects/planter-deploy.prefab",
            "assets/prefabs/deployable/playerioents/detectors/hbhfsensor/effects/detect_down.prefab",
            "assets/prefabs/deployable/playerioents/detectors/hbhfsensor/effects/detect_up.prefab",
            "assets/prefabs/deployable/quarry/effects/mining-quarry-deploy.prefab",
            "assets/prefabs/deployable/reactive target/effects/bullseye.prefab",
            "assets/prefabs/deployable/reactive target/effects/reactive-target-deploy.prefab",
            "assets/prefabs/deployable/reactive target/effects/snd_hit.prefab",
            "assets/prefabs/deployable/reactive target/effects/snd_knockdown.prefab",
            "assets/prefabs/deployable/reactive target/effects/snd_reset.prefab",
            "assets/prefabs/deployable/reactive target/effects/tire_smokepuff.prefab",
            "assets/prefabs/deployable/recycler/effects/start.prefab",
            "assets/prefabs/deployable/recycler/effects/stop.prefab",
            "assets/prefabs/deployable/repair bench/effects/repair-bench-deploy.prefab",
            "assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab",
            "assets/prefabs/deployable/research table/effects/research-fail.prefab",
            "assets/prefabs/deployable/research table/effects/research-start.prefab",
            "assets/prefabs/deployable/research table/effects/research-success.prefab",
            "assets/prefabs/deployable/research table/effects/research-table-deploy.prefab",
            "assets/prefabs/deployable/rug/effects/rug-deploy.prefab",
            "assets/prefabs/deployable/search light/effects/search-light-deploy.prefab",
            "assets/prefabs/deployable/shelves/effects/shelves-deploy.prefab",
            "assets/prefabs/deployable/signs/effects/large-banner-deploy.prefab",
            "assets/prefabs/deployable/signs/effects/metal-sign-deploy.prefab",
            "assets/prefabs/deployable/signs/effects/picture-frame-deploy.prefab",
            "assets/prefabs/deployable/signs/effects/sign-post-deploy.prefab",
            "assets/prefabs/deployable/signs/effects/town-sign-deploy.prefab",
            "assets/prefabs/deployable/signs/effects/wood-sign-deploy.prefab",
            "assets/prefabs/deployable/single shot trap/effects/shotgun-trap-deploy.prefab",
            "assets/prefabs/deployable/sleeping bag/effects/sleeping-bag-deploy.prefab",
            "assets/prefabs/deployable/small stash/effects/small-stash-deploy.prefab",
            "assets/prefabs/deployable/spinner_wheel/effects/spinner-wheel-deploy.prefab",
            "assets/prefabs/deployable/survivalfishtrap/effects/fish-trap-deploy.prefab",
            "assets/prefabs/deployable/survivalfishtrap/effects/fish_caught.prefab",
            "assets/prefabs/deployable/table/effects/table-deploy.prefab",
            "assets/prefabs/deployable/tier 1 workbench/effects/experiment-start.prefab",
            "assets/prefabs/deployable/tier 1 workbench/effects/workbench-tier-1-deploy.prefab",
            "assets/prefabs/deployable/tier 2 workbench/effects/workbench-tier-2-deploy.prefab",
            "assets/prefabs/deployable/tier 3 workbench/effects/workbench-tier-3-deploy.prefab",
            "assets/prefabs/deployable/tool cupboard/effects/tool-cupboard-deploy.prefab",
            "assets/prefabs/deployable/tuna can wall lamp/effects/tuna-can-lamp-deploy.prefab",
            "assets/prefabs/deployable/vendingmachine/effects/vending-machine-deploy.prefab",
            "assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab",
            "assets/prefabs/deployable/water catcher/effects/water-catcher-deploy.prefab",
            "assets/prefabs/deployable/water catcher/effects/water-catcher-large-deploy.prefab",
            "assets/prefabs/deployable/waterpurifier/effects/water-purifier-deploy.prefab",
            "assets/prefabs/deployable/woodenbox/effects/wooden-box-deploy.prefab",
            "assets/prefabs/effects/auroras/auroras_skychild.prefab",
            "assets/prefabs/effects/foliage/pfx_leaves.prefab",
            "assets/prefabs/effects/foliage/pfx_leaves_dead.prefab",
            "assets/prefabs/effects/foliage/pfx_leaves_reddish.prefab",
            "assets/prefabs/effects/foliage/pfx_leaves_yellowish.prefab",
            "assets/prefabs/effects/local camera particles/camfx_dust.prefab",
            "assets/prefabs/effects/local camera particles/camfx_rain.prefab",
            "assets/prefabs/effects/local camera particles/camfx_sand.prefab",
            "assets/prefabs/effects/local camera particles/camfx_snow.prefab",
            "assets/prefabs/effects/weather/pfx_duststorm.prefab",
            "assets/prefabs/effects/weather/pfx_sandstorm.prefab",
            "assets/prefabs/effects/weather/sandstorm.prefab",
            "assets/prefabs/food/bota bag/effects/bota-bag-cork-squeak.prefab",
            "assets/prefabs/food/bota bag/effects/bota-bag-deploy.prefab",
            "assets/prefabs/food/bota bag/effects/bota-bag-fill-container.prefab",
            "assets/prefabs/food/bota bag/effects/bota-bag-fill-world.prefab",
            "assets/prefabs/food/bota bag/effects/bota-bag-remove-cap.prefab",
            "assets/prefabs/food/bota bag/effects/bota-bag-slosh-fast.prefab",
            "assets/prefabs/food/bota bag/effects/bota-bag-slosh.prefab",
            "assets/prefabs/food/small water bottle/effects/water-bottle-deploy.prefab",
            "assets/prefabs/food/small water bottle/effects/water-bottle-fill-container.prefab",
            "assets/prefabs/food/small water bottle/effects/water-bottle-fill-world.prefab",
            "assets/prefabs/food/small water bottle/effects/water-bottle-remove-cap.prefab",
            "assets/prefabs/food/small water bottle/effects/water-bottle-slosh-fast.prefab",
            "assets/prefabs/food/small water bottle/effects/water-bottle-slosh.prefab",
            "assets/prefabs/food/water jug/effects/water-jug-deploy.prefab",
            "assets/prefabs/food/water jug/effects/water-jug-fill-container.prefab",
            "assets/prefabs/food/water jug/effects/water-jug-fill-world.prefab",
            "assets/prefabs/food/water jug/effects/water-jug-open-cap.prefab",
            "assets/prefabs/food/water jug/effects/water-jug-throw-water.prefab",
            "assets/prefabs/food/water jug/effects/waterjug_splash.prefab",
            "assets/prefabs/instruments/bass/effects/guitardeploy.prefab",
            "assets/prefabs/instruments/drumkit/effects/drumkit-deploy.prefab",
            "assets/prefabs/instruments/guitar/effects/guitardeploy.prefab",
            "assets/prefabs/instruments/jerrycanguitar/effects/guitardeploy.prefab",
            "assets/prefabs/instruments/piano/effects/piano-deploy.prefab",
            "assets/prefabs/instruments/xylophone/effects/xylophone-deploy.prefab",
            "assets/prefabs/locks/keypad/effects/lock-code-deploy.prefab",
            "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab",
            "assets/prefabs/locks/keypad/effects/lock.code.lock.prefab",
            "assets/prefabs/locks/keypad/effects/lock.code.shock.prefab",
            "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab",
            "assets/prefabs/locks/keypad/effects/lock.code.updated.prefab",
            "assets/prefabs/misc/blueprintbase/effects/blueprint_read.prefab",
            "assets/prefabs/misc/burlap sack/effects/phys-impact-hard.prefab",
            "assets/prefabs/misc/burlap sack/effects/phys-impact-med.prefab",
            "assets/prefabs/misc/burlap sack/effects/phys-impact-soft.prefab",
            "assets/prefabs/misc/chinesenewyear/dragondoorknocker/effects/door_knock_fx.prefab",
            "assets/prefabs/misc/chinesenewyear/throwablefirecrackers/effects/throw.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/add_egg.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/aim.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/aim_cancel.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/deploy.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/eggexplosion.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/grab_egg_start.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/place.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/place_grab_egg.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/raise.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/settle.prefab",
            "assets/prefabs/misc/easter/easter basket/effects/throw.prefab",
            "assets/prefabs/misc/easter/painted eggs/effects/bronze_open.prefab",
            "assets/prefabs/misc/easter/painted eggs/effects/egg_upgrade.prefab",
            "assets/prefabs/misc/easter/painted eggs/effects/eggpickup.prefab",
            "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab",
            "assets/prefabs/misc/easter/painted eggs/effects/silver_open.prefab",
            "assets/prefabs/misc/halloween/lootbag/effects/bronze_open.prefab",
            "assets/prefabs/misc/halloween/lootbag/effects/gold_open.prefab",
            "assets/prefabs/misc/halloween/lootbag/effects/loot_bag_upgrade.prefab",
            "assets/prefabs/misc/halloween/lootbag/effects/silver_open.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/add_egg.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/aim.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/aim_cancel.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/deploy.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/eggexplosion.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/grab_egg_start.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/place.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/place_grab_egg.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/raise.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/settle.prefab",
            "assets/prefabs/misc/halloween/pumpkin_bucket/effects/throw.prefab",
            "assets/prefabs/misc/halloween/skull_door_knocker/effects/door_knock_fx.prefab",
            "assets/prefabs/misc/halloween/skull_door_knocker/effects/skull_door_knock_fx.prefab",
            "assets/prefabs/misc/junkpile/effects/despawn.prefab",
            "assets/prefabs/misc/orebonus/effects/bonus_fail.prefab",
            "assets/prefabs/misc/orebonus/effects/bonus_finish.prefab",
            "assets/prefabs/misc/orebonus/effects/bonus_hit.prefab",
            "assets/prefabs/misc/orebonus/effects/hotspot_death.prefab",
            "assets/prefabs/misc/orebonus/effects/ore_finish.prefab",
            "assets/prefabs/misc/xmas/candy cane club/effects/attack-1.prefab",
            "assets/prefabs/misc/xmas/candy cane club/effects/attack-2.prefab",
            "assets/prefabs/misc/xmas/candy cane club/effects/deploy.prefab",
            "assets/prefabs/misc/xmas/candy cane club/effects/hit.prefab",
            "assets/prefabs/misc/xmas/candy cane club/effects/tap.prefab",
            "assets/prefabs/misc/xmas/candy cane club/effects/throw.prefab",
            "assets/prefabs/misc/xmas/presents/effects/unwrap.prefab",
            "assets/prefabs/misc/xmas/presents/effects/wrap.prefab",
            "assets/prefabs/misc/xmas/snowball/effects/attack.prefab",
            "assets/prefabs/misc/xmas/snowball/effects/deploy.prefab",
            "assets/prefabs/misc/xmas/snowball/effects/impact.prefab",
            "assets/prefabs/misc/xmas/snowball/effects/strike.prefab",
            "assets/prefabs/misc/xmas/snowball/effects/strike_screenshake.prefab",
            "assets/prefabs/misc/xmas/snowball/effects/throw.prefab",
            "assets/prefabs/npc/autoturret/effects/autoturret-deploy.prefab",
            "assets/prefabs/npc/autoturret/effects/offline.prefab",
            "assets/prefabs/npc/autoturret/effects/online.prefab",
            "assets/prefabs/npc/autoturret/effects/targetacquired.prefab",
            "assets/prefabs/npc/autoturret/effects/targetlost.prefab",
            "assets/prefabs/npc/ch47/effects/crashfire.prefab",
            "assets/prefabs/npc/ch47/effects/metaldebris-2.prefab",
            "assets/prefabs/npc/ch47/effects/metaldebris-3.prefab",
            "assets/prefabs/npc/ch47/effects/metaldebris.prefab",
            "assets/prefabs/npc/ch47/effects/watergroundeffect.prefab",
            "assets/prefabs/npc/flame turret/effects/flameturret-deploy.prefab",
            "assets/prefabs/npc/m2bradley/effects/bradley_explosion.prefab",
            "assets/prefabs/npc/m2bradley/effects/coaxmgmuzzle.prefab",
            "assets/prefabs/npc/m2bradley/effects/maincannonattack.prefab",
            "assets/prefabs/npc/m2bradley/effects/maincannonshell_explosion.prefab",
            "assets/prefabs/npc/m2bradley/effects/sidegun_muzzleflash.prefab",
            "assets/prefabs/npc/m2bradley/effects/tread_dirt.prefab",
            "assets/prefabs/npc/m2bradley/effects/tread_smoke.prefab",
            "assets/prefabs/npc/patrol helicopter/damage_effect_debris.prefab",
            "assets/prefabs/npc/patrol helicopter/effects/gun_fire.prefab",
            "assets/prefabs/npc/patrol helicopter/effects/gun_fire_small.prefab",
            "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab",
            "assets/prefabs/npc/patrol helicopter/effects/rocket_airburst_explosion.prefab",
            "assets/prefabs/npc/patrol helicopter/effects/rocket_airburst_groundeffect.prefab",
            "assets/prefabs/npc/patrol helicopter/effects/rocket_explosion.prefab",
            "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab",
            "assets/prefabs/npc/patrol helicopter/groundeffect.prefab",
            "assets/prefabs/npc/sam_site_turret/effects/rocket_sam_explosion.prefab",
            "assets/prefabs/npc/sam_site_turret/effects/sam_damage.prefab",
            "assets/prefabs/npc/sam_site_turret/effects/tube_launch.prefab",
            "assets/prefabs/plants/plantharvest.effect.prefab",
            "assets/prefabs/plants/plantseed.effect.prefab",
            "assets/prefabs/tools/binoculars/effects/deploy.prefab",
            "assets/prefabs/tools/c4/effects/c4_explosion.prefab",
            "assets/prefabs/tools/c4/effects/c4_stick.prefab",
            "assets/prefabs/tools/c4/effects/deploy.prefab",
            "assets/prefabs/tools/detonator/effects/attack.prefab",
            "assets/prefabs/tools/detonator/effects/deploy.prefab",
            "assets/prefabs/tools/detonator/effects/unpress.prefab",
            "assets/prefabs/tools/flareold/effects/deploy.prefab",
            "assets/prefabs/tools/flareold/effects/ignite.prefab",
            "assets/prefabs/tools/flareold/effects/popcap.prefab",
            "assets/prefabs/tools/flareold/effects/pullpin.prefab",
            "assets/prefabs/tools/flareold/effects/throw.prefab",
            "assets/prefabs/tools/flashlight/effects/attack.prefab",
            "assets/prefabs/tools/flashlight/effects/attack_hit.prefab",
            "assets/prefabs/tools/flashlight/effects/deploy.prefab",
            "assets/prefabs/tools/flashlight/effects/turn_on.prefab",
            "assets/prefabs/tools/jackhammer/effects/deploy.prefab",
            "assets/prefabs/tools/jackhammer/effects/strike_screenshake.prefab",
            "assets/prefabs/tools/keycard/effects/attack.prefab",
            "assets/prefabs/tools/keycard/effects/deploy.prefab",
            "assets/prefabs/tools/keycard/effects/swipe.prefab",
            "assets/prefabs/tools/medical syringe/effects/inject_friend.prefab",
            "assets/prefabs/tools/medical syringe/effects/inject_self.prefab",
            "assets/prefabs/tools/medical syringe/effects/pop_button_cap.prefab",
            "assets/prefabs/tools/medical syringe/effects/pop_cap.prefab",
            "assets/prefabs/tools/pager/effects/beep.prefab",
            "assets/prefabs/tools/pager/effects/vibrate.prefab",
            "assets/prefabs/tools/smoke grenade/effects/ignite.prefab",
            "assets/prefabs/tools/smoke grenade/effects/smokegrenade.prefab",
            "assets/prefabs/tools/smoke grenade/effects/smokegrenade_small.prefab",
            "assets/prefabs/tools/surveycharge/effects/deploy.prefab",
            "assets/prefabs/tools/wire/effects/plugeffect.prefab",
            "assets/prefabs/weapon mods/flashlight/lighteffect_1p.prefab",
            "assets/prefabs/weapon mods/flashlight/lighteffect_3p.prefab",
            "assets/prefabs/weapon mods/lasersight/lasereffect_1p.prefab",
            "assets/prefabs/weapon mods/lasersight/lasereffect_3p.prefab",
            "assets/prefabs/weapon mods/mod_attach.fx.prefab",
            "assets/prefabs/weapon mods/silencers/effects/silencedshot_default.prefab",
            "assets/prefabs/weapon mods/silencers/effects/silencer_attach.fx.prefab",
            "assets/prefabs/weapons/ak47u/effects/attack.prefab",
            "assets/prefabs/weapons/ak47u/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/ak47u/effects/attack_shake.prefab",
            "assets/prefabs/weapons/ak47u/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/ak47u/effects/bolt_back.prefab",
            "assets/prefabs/weapons/ak47u/effects/bolt_forward.prefab",
            "assets/prefabs/weapons/ak47u/effects/deploy.prefab",
            "assets/prefabs/weapons/ak47u/effects/dryfire.prefab",
            "assets/prefabs/weapons/ak47u/effects/eject_rifle_shell.prefab",
            "assets/prefabs/weapons/ak47u/effects/grab_magazine.prefab",
            "assets/prefabs/weapons/ak47u/effects/insert_magazine.prefab",
            "assets/prefabs/weapons/ak47u/effects/phys-impact-hard.prefab",
            "assets/prefabs/weapons/ak47u/effects/phys-impact-med.prefab",
            "assets/prefabs/weapons/ak47u/effects/phys-impact-soft.prefab",
            "assets/prefabs/weapons/ak47u/effects/reload_boltaction.prefab",
            "assets/prefabs/weapons/ak47u/effects/reload_start.prefab",
            "assets/prefabs/weapons/ak47u/effects/w_drop_magazine.prefab",
            "assets/prefabs/weapons/ak47u/effects/w_eject_rifle_shell.prefab",
            "assets/prefabs/weapons/arms/effects/drop_item.prefab",
            "assets/prefabs/weapons/arms/effects/hook-1.prefab",
            "assets/prefabs/weapons/arms/effects/hook-2.prefab",
            "assets/prefabs/weapons/arms/effects/hook_hit-1.prefab",
            "assets/prefabs/weapons/arms/effects/hook_hit-2.prefab",
            "assets/prefabs/weapons/arms/effects/jab-1.prefab",
            "assets/prefabs/weapons/arms/effects/jab-2.prefab",
            "assets/prefabs/weapons/arms/effects/jab-3.prefab",
            "assets/prefabs/weapons/arms/effects/jab_hit-1.prefab",
            "assets/prefabs/weapons/arms/effects/jab_hit-2.prefab",
            "assets/prefabs/weapons/arms/effects/pickup_item.prefab",
            "assets/prefabs/weapons/arms/effects/shove.prefab",
            "assets/prefabs/weapons/arms/effects/uppercut.prefab",
            "assets/prefabs/weapons/arms/effects/uppercut_hit-1.prefab",
            "assets/prefabs/weapons/bandage/effects/deploy.prefab",
            "assets/prefabs/weapons/bandage/effects/wraparm.prefab",
            "assets/prefabs/weapons/bandage/effects/wraphead.prefab",
            "assets/prefabs/weapons/bandage/effects/wrapother.prefab",
            "assets/prefabs/weapons/beancan grenade/effects/beancan_grenade_explosion.prefab",
            "assets/prefabs/weapons/beancan grenade/effects/bounce.prefab",
            "assets/prefabs/weapons/beancan grenade/effects/deploy.prefab",
            "assets/prefabs/weapons/beancan grenade/effects/light_fuse.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/attack.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/attack_shake.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/boltback.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/boltforward.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/deploy.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/dryfire.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/eject_rifle_shell.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/holster.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/insertbullet.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/pfx_bolt_open_smoke.prefab",
            "assets/prefabs/weapons/bolt rifle/effects/w_eject_rifle_shell.prefab",
            "assets/prefabs/weapons/bone club/effects/attack-1.prefab",
            "assets/prefabs/weapons/bone club/effects/attack-2.prefab",
            "assets/prefabs/weapons/bone club/effects/deploy.prefab",
            "assets/prefabs/weapons/bone club/effects/hit.prefab",
            "assets/prefabs/weapons/bone club/effects/tap.prefab",
            "assets/prefabs/weapons/bone club/effects/throw.prefab",
            "assets/prefabs/weapons/bone knife/effects/attack-1.prefab",
            "assets/prefabs/weapons/bone knife/effects/attack-2.prefab",
            "assets/prefabs/weapons/bone knife/effects/deploy.prefab",
            "assets/prefabs/weapons/bone knife/effects/hit.prefab",
            "assets/prefabs/weapons/bone knife/effects/holster.prefab",
            "assets/prefabs/weapons/bone knife/effects/strike-soft.prefab",
            "assets/prefabs/weapons/bone knife/effects/strike.prefab",
            "assets/prefabs/weapons/bone knife/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/bone knife/effects/throw.prefab",
            "assets/prefabs/weapons/bow/effects/attack.prefab",
            "assets/prefabs/weapons/bow/effects/deploy.prefab",
            "assets/prefabs/weapons/bow/effects/draw_arrow.prefab",
            "assets/prefabs/weapons/bow/effects/draw_cancel.prefab",
            "assets/prefabs/weapons/bow/effects/fire.prefab",
            "assets/prefabs/weapons/cake/effects/attack.prefab",
            "assets/prefabs/weapons/cake/effects/deploy.prefab",
            "assets/prefabs/weapons/cake/effects/strike.prefab",
            "assets/prefabs/weapons/cake/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/cake/effects/throw.prefab",
            "assets/prefabs/weapons/chainsaw/effects/chainlink_hit_blood.prefab",
            "assets/prefabs/weapons/chainsaw/effects/chainlink_hit_wood.prefab",
            "assets/prefabs/weapons/chainsaw/effects/chainlink_smoke.prefab",
            "assets/prefabs/weapons/chainsaw/effects/deploy.prefab",
            "assets/prefabs/weapons/chainsaw/effects/fill-gas.prefab",
            "assets/prefabs/weapons/chainsaw/effects/ignition.prefab",
            "assets/prefabs/weapons/chainsaw/effects/unscrew-cap.prefab",
            "assets/prefabs/weapons/cleaver big/effects/attack-1.prefab",
            "assets/prefabs/weapons/cleaver big/effects/attack-2.prefab",
            "assets/prefabs/weapons/cleaver big/effects/attack-3.prefab",
            "assets/prefabs/weapons/cleaver big/effects/deploy.prefab",
            "assets/prefabs/weapons/cleaver big/effects/hit-soft.prefab",
            "assets/prefabs/weapons/cleaver big/effects/hit.prefab",
            "assets/prefabs/weapons/cleaver big/effects/throw.prefab",
            "assets/prefabs/weapons/compound bow/effects/attack.prefab",
            "assets/prefabs/weapons/compound bow/effects/deploy.prefab",
            "assets/prefabs/weapons/compound bow/effects/draw_cancel.prefab",
            "assets/prefabs/weapons/compound bow/effects/initial_pullback.prefab",
            "assets/prefabs/weapons/compound bow/effects/place_arrow.prefab",
            "assets/prefabs/weapons/compound bow/effects/reload_start.prefab",
            "assets/prefabs/weapons/crossbow/effects/attack.prefab",
            "assets/prefabs/weapons/crossbow/effects/deploy.prefab",
            "assets/prefabs/weapons/crossbow/effects/dryfire.prefab",
            "assets/prefabs/weapons/crossbow/effects/reload.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/attack.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/attack_shake.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/bolt_back.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/bolt_shut.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/close_barrel.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/deploy.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/dryfire.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/dump_shells.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/insert_shells.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/open_barrel.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/pfx_bolt_shut_sparks.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/pfx_open_barrel_smoke.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/w_eject_shotgun_shell_1.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/w_eject_shotgun_shell_2.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/attack.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/attack_shake.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/deploy.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/flint_spark.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/holster.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/insert_bullet.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/push_barrel.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/rock_scrape-1.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/rock_scrape-2.prefab",
            "assets/prefabs/weapons/eoka pistol/effects/rock_scrape-3.prefab",
            "assets/prefabs/weapons/f1 grenade/effects/bounce.prefab",
            "assets/prefabs/weapons/f1 grenade/effects/deploy.prefab",
            "assets/prefabs/weapons/f1 grenade/effects/f1grenade_explosion.prefab",
            "assets/prefabs/weapons/f1 grenade/effects/holster.prefab",
            "assets/prefabs/weapons/f1 grenade/effects/pullpin.prefab",
            "assets/prefabs/weapons/f1 grenade/effects/throw.prefab",
            "assets/prefabs/weapons/flamethrower/effects/deploy.prefab",
            "assets/prefabs/weapons/flamethrower/effects/flame_explosion 1.prefab",
            "assets/prefabs/weapons/flamethrower/effects/flame_explosion.prefab",
            "assets/prefabs/weapons/flamethrower/effects/flame_explosion_dist 1.prefab",
            "assets/prefabs/weapons/flamethrower/effects/flame_explosion_dist.prefab",
            "assets/prefabs/weapons/flamethrower/effects/flametest.prefab",
            "assets/prefabs/weapons/flamethrower/effects/flamethrowerflamefx-v2-1stperson.prefab",
            "assets/prefabs/weapons/flamethrower/effects/flamethrowerflamefx-v2-1stperson2.prefab",
            "assets/prefabs/weapons/flamethrower/effects/flamethrowerflamefx-v2.prefab",
            "assets/prefabs/weapons/flamethrower/effects/gas_release.prefab",
            "assets/prefabs/weapons/flamethrower/effects/gascan_in.prefab",
            "assets/prefabs/weapons/flamethrower/effects/gascan_out.prefab",
            "assets/prefabs/weapons/flamethrower/effects/toggle_flame.prefab",
            "assets/prefabs/weapons/flamethrower/effects/valve_open.prefab",
            "assets/prefabs/weapons/grenade launcher/effects/attack.prefab",
            "assets/prefabs/weapons/grenade launcher/effects/deploy.prefab",
            "assets/prefabs/weapons/grenade launcher/effects/reload_end.prefab",
            "assets/prefabs/weapons/grenade launcher/effects/reload_single_insert.prefab",
            "assets/prefabs/weapons/grenade launcher/effects/reload_single_spin.prefab",
            "assets/prefabs/weapons/grenade launcher/effects/reload_single_start.prefab",
            "assets/prefabs/weapons/grenade launcher/effects/reload_start.prefab",
            "assets/prefabs/weapons/hacksaw/effects/attack-1.prefab",
            "assets/prefabs/weapons/hacksaw/effects/attack-2.prefab",
            "assets/prefabs/weapons/hacksaw/effects/hit.prefab",
            "assets/prefabs/weapons/halloween/butcher knife/effects/attack-1.prefab",
            "assets/prefabs/weapons/halloween/butcher knife/effects/attack-2.prefab",
            "assets/prefabs/weapons/halloween/butcher knife/effects/deploy.prefab",
            "assets/prefabs/weapons/halloween/butcher knife/effects/hit.prefab",
            "assets/prefabs/weapons/halloween/butcher knife/effects/holster.prefab",
            "assets/prefabs/weapons/halloween/butcher knife/effects/strike-soft.prefab",
            "assets/prefabs/weapons/halloween/butcher knife/effects/strike.prefab",
            "assets/prefabs/weapons/halloween/butcher knife/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/halloween/butcher knife/effects/throw.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/2hand_deploy.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/attack.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/deploy.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/holster.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/pull_out.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/strike_stone-muted.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/strike_stone-soft.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/strike_stone.prefab",
            "assets/prefabs/weapons/halloween/pitchfork/effects/throw.prefab",
            "assets/prefabs/weapons/halloween/sickle/effects/attack_shake.prefab",
            "assets/prefabs/weapons/halloween/sickle/effects/deploy.prefab",
            "assets/prefabs/weapons/halloween/sickle/effects/strike-muted.prefab",
            "assets/prefabs/weapons/halloween/sickle/effects/strike-soft.prefab",
            "assets/prefabs/weapons/halloween/sickle/effects/strike.prefab",
            "assets/prefabs/weapons/halloween/sickle/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/halloween/sickle/effects/swing.prefab",
            "assets/prefabs/weapons/halloween/sickle/effects/throw.prefab",
            "assets/prefabs/weapons/hammer/effects/attack.prefab",
            "assets/prefabs/weapons/hammer/effects/deploy.prefab",
            "assets/prefabs/weapons/hammer/effects/holster.prefab",
            "assets/prefabs/weapons/hammer/effects/strike.prefab",
            "assets/prefabs/weapons/hammer/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/hammer/effects/throw.prefab",
            "assets/prefabs/weapons/hatchet/effects/attack_shake.prefab",
            "assets/prefabs/weapons/hatchet/effects/deploy.prefab",
            "assets/prefabs/weapons/hatchet/effects/strike-muted.prefab",
            "assets/prefabs/weapons/hatchet/effects/strike-soft.prefab",
            "assets/prefabs/weapons/hatchet/effects/strike.prefab",
            "assets/prefabs/weapons/hatchet/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/hatchet/effects/swing.prefab",
            "assets/prefabs/weapons/hatchet/effects/throw.prefab",
            "assets/prefabs/weapons/knife/effects/attack-1.prefab",
            "assets/prefabs/weapons/knife/effects/attack-2.prefab",
            "assets/prefabs/weapons/knife/effects/deploy.prefab",
            "assets/prefabs/weapons/knife/effects/hit.prefab",
            "assets/prefabs/weapons/knife/effects/holster.prefab",
            "assets/prefabs/weapons/knife/effects/strike-soft.prefab",
            "assets/prefabs/weapons/knife/effects/strike.prefab",
            "assets/prefabs/weapons/knife/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/knife/effects/throw.prefab",
            "assets/prefabs/weapons/l96/effects/attack.prefab",
            "assets/prefabs/weapons/l96/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/l96/effects/attack_shake.prefab",
            "assets/prefabs/weapons/l96/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/l96/effects/dryfire.prefab",
            "assets/prefabs/weapons/l96/effects/eject_rifle_shell.prefab",
            "assets/prefabs/weapons/l96/effects/l96_bolt_action.prefab",
            "assets/prefabs/weapons/l96/effects/l96_bolt_finish.prefab",
            "assets/prefabs/weapons/l96/effects/l96_bolt_grab.prefab",
            "assets/prefabs/weapons/l96/effects/l96_bolt_start.prefab",
            "assets/prefabs/weapons/l96/effects/l96_deploy.prefab",
            "assets/prefabs/weapons/l96/effects/l96_insert_mag.prefab",
            "assets/prefabs/weapons/l96/effects/l96_reload_finish.prefab",
            "assets/prefabs/weapons/l96/effects/l96_reload_start.prefab",
            "assets/prefabs/weapons/l96/effects/l96_remove_mag.prefab",
            "assets/prefabs/weapons/lr300/effects/attack.prefab",
            "assets/prefabs/weapons/lr300/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/lr300/effects/attack_shake.prefab",
            "assets/prefabs/weapons/lr300/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/lr300/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/lr300/effects/bolt_catch.prefab",
            "assets/prefabs/weapons/lr300/effects/buttstock_extend.prefab",
            "assets/prefabs/weapons/lr300/effects/buttstock_swingback.prefab",
            "assets/prefabs/weapons/lr300/effects/buttstock_unlock.prefab",
            "assets/prefabs/weapons/lr300/effects/charging_handle_back.prefab",
            "assets/prefabs/weapons/lr300/effects/charging_handle_shut.prefab",
            "assets/prefabs/weapons/lr300/effects/clip_in.prefab",
            "assets/prefabs/weapons/lr300/effects/clip_out.prefab",
            "assets/prefabs/weapons/lr300/effects/deploy.prefab",
            "assets/prefabs/weapons/lr300/effects/dryfire.prefab",
            "assets/prefabs/weapons/lr300/effects/eject_rifle_shell.prefab",
            "assets/prefabs/weapons/lr300/effects/grab_magazine.prefab",
            "assets/prefabs/weapons/lr300/effects/pfx_clip_out_smoke.prefab",
            "assets/prefabs/weapons/lr300/effects/w_drop_magazine.prefab",
            "assets/prefabs/weapons/lr300/effects/w_eject_rifle_shell.prefab",
            "assets/prefabs/weapons/m249/effects/ammobox_insert.prefab",
            "assets/prefabs/weapons/m249/effects/ammobox_remove.prefab",
            "assets/prefabs/weapons/m249/effects/attack.prefab",
            "assets/prefabs/weapons/m249/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/m249/effects/attack_shake.prefab",
            "assets/prefabs/weapons/m249/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/m249/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/m249/effects/bolt_back.prefab",
            "assets/prefabs/weapons/m249/effects/bolt_forward.prefab",
            "assets/prefabs/weapons/m249/effects/chainbelt.prefab",
            "assets/prefabs/weapons/m249/effects/deploy.prefab",
            "assets/prefabs/weapons/m249/effects/dryfire.prefab",
            "assets/prefabs/weapons/m249/effects/eject_beltlink.prefab",
            "assets/prefabs/weapons/m249/effects/eject_rifle_shell.prefab",
            "assets/prefabs/weapons/m249/effects/place_bullets.prefab",
            "assets/prefabs/weapons/m249/effects/reload_smoke.prefab",
            "assets/prefabs/weapons/m249/effects/topcover_close.prefab",
            "assets/prefabs/weapons/m249/effects/topcover_open.prefab",
            "assets/prefabs/weapons/m249/effects/w_drop_magazine.prefab",
            "assets/prefabs/weapons/m249/effects/w_eject_rifle_shell.prefab",
            "assets/prefabs/weapons/m39 emr/effects/attack.prefab",
            "assets/prefabs/weapons/m39 emr/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/m39 emr/effects/attack_shake.prefab",
            "assets/prefabs/weapons/m39 emr/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/m39 emr/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/m39 emr/effects/bolt_action.prefab",
            "assets/prefabs/weapons/m39 emr/effects/clip_in.prefab",
            "assets/prefabs/weapons/m39 emr/effects/clip_out.prefab",
            "assets/prefabs/weapons/m39 emr/effects/clip_slap.prefab",
            "assets/prefabs/weapons/m39 emr/effects/deploy.prefab",
            "assets/prefabs/weapons/m39 emr/effects/deploy_grab_forearm.prefab",
            "assets/prefabs/weapons/m39 emr/effects/dryfire.prefab",
            "assets/prefabs/weapons/m39 emr/effects/eject_rifle_shell.prefab",
            "assets/prefabs/weapons/m39 emr/effects/reload_start.prefab",
            "assets/prefabs/weapons/m39 emr/effects/w_drop_magazine.prefab",
            "assets/prefabs/weapons/m39 emr/effects/w_eject_rifle_shell.prefab",
            "assets/prefabs/weapons/m92/effects/attack.prefab",
            "assets/prefabs/weapons/m92/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/m92/effects/attack_shake.prefab",
            "assets/prefabs/weapons/m92/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/m92/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/m92/effects/clipin.prefab",
            "assets/prefabs/weapons/m92/effects/clipout.prefab",
            "assets/prefabs/weapons/m92/effects/deploy.prefab",
            "assets/prefabs/weapons/m92/effects/dryfire.prefab",
            "assets/prefabs/weapons/m92/effects/eject_pistol_shell.prefab",
            "assets/prefabs/weapons/m92/effects/pfx_ejectshell_smoke.prefab",
            "assets/prefabs/weapons/m92/effects/safety.prefab",
            "assets/prefabs/weapons/m92/effects/slideopen.prefab",
            "assets/prefabs/weapons/m92/effects/slideshut.prefab",
            "assets/prefabs/weapons/m92/effects/w_drop_mag.prefab",
            "assets/prefabs/weapons/m92/effects/w_eject_pistol_shell.prefab",
            "assets/prefabs/weapons/mace/effects/attack-1.prefab",
            "assets/prefabs/weapons/mace/effects/attack-2.prefab",
            "assets/prefabs/weapons/mace/effects/deploy.prefab",
            "assets/prefabs/weapons/mace/effects/hit.prefab",
            "assets/prefabs/weapons/mace/effects/throw.prefab",
            "assets/prefabs/weapons/machete/effects/attack-1.prefab",
            "assets/prefabs/weapons/machete/effects/attack-2.prefab",
            "assets/prefabs/weapons/machete/effects/attack-3.prefab",
            "assets/prefabs/weapons/machete/effects/deploy.prefab",
            "assets/prefabs/weapons/machete/effects/hit-muted.prefab",
            "assets/prefabs/weapons/machete/effects/hit-soft.prefab",
            "assets/prefabs/weapons/machete/effects/hit.prefab",
            "assets/prefabs/weapons/machete/effects/swing_thirdperson.prefab",
            "assets/prefabs/weapons/machete/effects/throw.prefab",
            "assets/prefabs/weapons/mp5/effects/attack.prefab",
            "assets/prefabs/weapons/mp5/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/mp5/effects/attack_shake.prefab",
            "assets/prefabs/weapons/mp5/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/mp5/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/mp5/effects/bolt_back.prefab",
            "assets/prefabs/weapons/mp5/effects/bolt_shut.prefab",
            "assets/prefabs/weapons/mp5/effects/bolt_slap.prefab",
            "assets/prefabs/weapons/mp5/effects/clip_in.prefab",
            "assets/prefabs/weapons/mp5/effects/clip_out.prefab",
            "assets/prefabs/weapons/mp5/effects/deploy.prefab",
            "assets/prefabs/weapons/mp5/effects/dryfire.prefab",
            "assets/prefabs/weapons/mp5/effects/eject_shell.prefab",
            "assets/prefabs/weapons/mp5/effects/fire_select.prefab",
            "assets/prefabs/weapons/mp5/effects/muzzleflash_flamelet.prefab",
            "assets/prefabs/weapons/mp5/effects/w_drop_magazine.prefab",
            "assets/prefabs/weapons/mp5/effects/w_eject_pistol_shell.prefab",
            "assets/prefabs/weapons/nailgun/effects/attack.prefab",
            "assets/prefabs/weapons/nailgun/effects/attack_shake.prefab",
            "assets/prefabs/weapons/nailgun/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/nailgun/effects/clip_in.prefab",
            "assets/prefabs/weapons/nailgun/effects/clip_out.prefab",
            "assets/prefabs/weapons/nailgun/effects/deploy.prefab",
            "assets/prefabs/weapons/pickaxe/effects/attack.prefab",
            "assets/prefabs/weapons/pickaxe/effects/deploy.prefab",
            "assets/prefabs/weapons/pickaxe/effects/strike-muted.prefab",
            "assets/prefabs/weapons/pickaxe/effects/strike-soft.prefab",
            "assets/prefabs/weapons/pickaxe/effects/strike.prefab",
            "assets/prefabs/weapons/pickaxe/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/pickaxe/effects/throw.prefab",
            "assets/prefabs/weapons/pipe shotgun/effects/attack.prefab",
            "assets/prefabs/weapons/pipe shotgun/effects/attack_shake.prefab",
            "assets/prefabs/weapons/pipe shotgun/effects/close_pipe.prefab",
            "assets/prefabs/weapons/pipe shotgun/effects/deploy.prefab",
            "assets/prefabs/weapons/pipe shotgun/effects/dryfire.prefab",
            "assets/prefabs/weapons/pipe shotgun/effects/holster.prefab",
            "assets/prefabs/weapons/pipe shotgun/effects/insert_shell.prefab",
            "assets/prefabs/weapons/pipe shotgun/effects/pfx_open_barrel_smoke.prefab",
            "assets/prefabs/weapons/pipe shotgun/effects/reload_start.prefab",
            "assets/prefabs/weapons/python/effects/attack.prefab",
            "assets/prefabs/weapons/python/effects/attack_shake.prefab",
            "assets/prefabs/weapons/python/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/python/effects/close_cylinder.prefab",
            "assets/prefabs/weapons/python/effects/deploy.prefab",
            "assets/prefabs/weapons/python/effects/dryfire.prefab",
            "assets/prefabs/weapons/python/effects/eject_shells.prefab",
            "assets/prefabs/weapons/python/effects/insert_shells.prefab",
            "assets/prefabs/weapons/python/effects/open_cylinder.prefab",
            "assets/prefabs/weapons/python/effects/pfx_open_cylinder_smoke.prefab",
            "assets/prefabs/weapons/python/effects/w_eject_pistol_shells.prefab",
            "assets/prefabs/weapons/revolver/effects/attack.prefab",
            "assets/prefabs/weapons/revolver/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/revolver/effects/attack_shake.prefab",
            "assets/prefabs/weapons/revolver/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/revolver/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/revolver/effects/deploy.prefab",
            "assets/prefabs/weapons/revolver/effects/dryfire.prefab",
            "assets/prefabs/weapons/revolver/effects/eject_shells.prefab",
            "assets/prefabs/weapons/revolver/effects/insert_shells.prefab",
            "assets/prefabs/weapons/revolver/effects/open_cylinder.prefab",
            "assets/prefabs/weapons/revolver/effects/prime_striker.prefab",
            "assets/prefabs/weapons/revolver/effects/shut_cylinder.prefab",
            "assets/prefabs/weapons/revolver/effects/w_eject_pistol_shells.prefab",
            "assets/prefabs/weapons/rock/effects/attack.prefab",
            "assets/prefabs/weapons/rock/effects/deploy.prefab",
            "assets/prefabs/weapons/rock/effects/strike.prefab",
            "assets/prefabs/weapons/rock/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/rock/effects/throw.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/attack.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/deploy.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/dryfire.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/fire.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/grab_handle.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/holster.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/pfx_close_hatch_smoke.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/pfx_fire_rocket_smokeout.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/pfx_open_hatch_smokeout.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/pfx_rocket_insert_smoke.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/pfx_rocket_insert_sparks.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/reload_begin.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/reload_close_hatch.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/reload_end.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/reload_insert_rocket.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/reload_open_hatch.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/rocket_explosion.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/rocket_explosion_incendiary.prefab",
            "assets/prefabs/weapons/rocketlauncher/effects/rocket_launch_fx.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/attack1.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/attack2.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/deploy.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/holster.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/strike-muted.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/strike-soft.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/strike.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/tap.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/throw.prefab",
            "assets/prefabs/weapons/salvaged_hammer/effects/attack1.prefab",
            "assets/prefabs/weapons/salvaged_hammer/effects/attack2.prefab",
            "assets/prefabs/weapons/salvaged_hammer/effects/deploy.prefab",
            "assets/prefabs/weapons/salvaged_hammer/effects/holster.prefab",
            "assets/prefabs/weapons/salvaged_hammer/effects/strike.prefab",
            "assets/prefabs/weapons/salvaged_hammer/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/salvaged_hammer/effects/throw.prefab",
            "assets/prefabs/weapons/salvaged_icepick/effects/attack.prefab",
            "assets/prefabs/weapons/salvaged_icepick/effects/deploy.prefab",
            "assets/prefabs/weapons/salvaged_icepick/effects/holster.prefab",
            "assets/prefabs/weapons/salvaged_icepick/effects/strike-muted.prefab",
            "assets/prefabs/weapons/salvaged_icepick/effects/strike-soft.prefab",
            "assets/prefabs/weapons/salvaged_icepick/effects/strike.prefab",
            "assets/prefabs/weapons/salvaged_icepick/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/salvaged_icepick/effects/tap.prefab",
            "assets/prefabs/weapons/salvaged_icepick/effects/throw.prefab",
            "assets/prefabs/weapons/satchelcharge/effects/deploy.prefab",
            "assets/prefabs/weapons/satchelcharge/effects/satchel-charge-explosion.prefab",
            "assets/prefabs/weapons/satchelcharge/effects/throw.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/attack.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/attack_pumpaction.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/attack_shake.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/deploy.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/deploy2.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/dryfire.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/flipover.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/insert_shell.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/pump_forward.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/reload_start.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/shell_smoke.prefab",
            "assets/prefabs/weapons/sawnoff_shotgun/effects/w_eject_shotgun_shell.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/attack.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/attack_shake.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/deploy.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/dryfire.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/eject_clip.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/eject_pistol_shell.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/grab_clip.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/insert_clip.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/slide_back.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/slide_shut.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/w_drop_mag.prefab",
            "assets/prefabs/weapons/semi auto pistol/effects/w_eject_pistol_shell.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/attack.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/attack_shake.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/bolt_back.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/bolt_forward.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/clip_in.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/clip_out.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/clip_slap.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/deploy.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/deploy_grab_forearm.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/dryfire.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/eject_rifle_shell.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/w_drop_magazine.prefab",
            "assets/prefabs/weapons/semi auto rifle/effects/w_eject_rifle_shell.prefab",
            "assets/prefabs/weapons/smg/effects/attack.prefab",
            "assets/prefabs/weapons/smg/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/smg/effects/attack_shake.prefab",
            "assets/prefabs/weapons/smg/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/smg/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/smg/effects/bolt_back.prefab",
            "assets/prefabs/weapons/smg/effects/bolt_shut.prefab",
            "assets/prefabs/weapons/smg/effects/clip_in.prefab",
            "assets/prefabs/weapons/smg/effects/clip_out.prefab",
            "assets/prefabs/weapons/smg/effects/deploy.prefab",
            "assets/prefabs/weapons/smg/effects/dryfire.prefab",
            "assets/prefabs/weapons/smg/effects/eject_shell.prefab",
            "assets/prefabs/weapons/smg/effects/reload_start.prefab",
            "assets/prefabs/weapons/smg/effects/w_drop_magazine.prefab",
            "assets/prefabs/weapons/smg/effects/w_eject_pistol_shell.prefab",
            "assets/prefabs/weapons/spas12/effects/attack.prefab",
            "assets/prefabs/weapons/spas12/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/spas12/effects/attack_shake.prefab",
            "assets/prefabs/weapons/spas12/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/spas12/effects/deploy.prefab",
            "assets/prefabs/weapons/spas12/effects/dryfire.prefab",
            "assets/prefabs/weapons/spas12/effects/eject_shell.prefab",
            "assets/prefabs/weapons/spas12/effects/insert_shell.prefab",
            "assets/prefabs/weapons/spas12/effects/insert_shell_breach.prefab",
            "assets/prefabs/weapons/spas12/effects/pump_back.prefab",
            "assets/prefabs/weapons/spas12/effects/pump_forward.prefab",
            "assets/prefabs/weapons/spas12/effects/w_eject_shotgun_shell.prefab",
            "assets/prefabs/weapons/stone hatchet/effects/attack_shake.prefab",
            "assets/prefabs/weapons/stone hatchet/effects/deploy.prefab",
            "assets/prefabs/weapons/stone hatchet/effects/strike-muted.prefab",
            "assets/prefabs/weapons/stone hatchet/effects/strike-soft.prefab",
            "assets/prefabs/weapons/stone hatchet/effects/strike.prefab",
            "assets/prefabs/weapons/stone hatchet/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/stone hatchet/effects/swing.prefab",
            "assets/prefabs/weapons/stone hatchet/effects/throw.prefab",
            "assets/prefabs/weapons/stone pickaxe/effects/attack.prefab",
            "assets/prefabs/weapons/stone pickaxe/effects/deploy.prefab",
            "assets/prefabs/weapons/stone pickaxe/effects/strike-muted.prefab",
            "assets/prefabs/weapons/stone pickaxe/effects/strike-soft.prefab",
            "assets/prefabs/weapons/stone pickaxe/effects/strike.prefab",
            "assets/prefabs/weapons/stone pickaxe/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/stone pickaxe/effects/swing.prefab",
            "assets/prefabs/weapons/stone pickaxe/effects/throw.prefab",
            "assets/prefabs/weapons/stone spear/effects/2hand_deploy.prefab",
            "assets/prefabs/weapons/stone spear/effects/attack.prefab",
            "assets/prefabs/weapons/stone spear/effects/deploy.prefab",
            "assets/prefabs/weapons/stone spear/effects/holster.prefab",
            "assets/prefabs/weapons/stone spear/effects/pull_out.prefab",
            "assets/prefabs/weapons/stone spear/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/stone spear/effects/strike_stone-muted.prefab",
            "assets/prefabs/weapons/stone spear/effects/strike_stone-soft.prefab",
            "assets/prefabs/weapons/stone spear/effects/strike_stone.prefab",
            "assets/prefabs/weapons/stone spear/effects/throw.prefab",
            "assets/prefabs/weapons/sword big/effects/attack-1.prefab",
            "assets/prefabs/weapons/sword big/effects/attack-2.prefab",
            "assets/prefabs/weapons/sword big/effects/attack-3.prefab",
            "assets/prefabs/weapons/sword big/effects/deploy.prefab",
            "assets/prefabs/weapons/sword big/effects/hit-muted.prefab",
            "assets/prefabs/weapons/sword big/effects/hit-soft.prefab",
            "assets/prefabs/weapons/sword big/effects/hit.prefab",
            "assets/prefabs/weapons/sword big/effects/throw.prefab",
            "assets/prefabs/weapons/sword/effects/attack-1.prefab",
            "assets/prefabs/weapons/sword/effects/attack-2.prefab",
            "assets/prefabs/weapons/sword/effects/attack-3.prefab",
            "assets/prefabs/weapons/sword/effects/deploy.prefab",
            "assets/prefabs/weapons/sword/effects/hit-muted.prefab",
            "assets/prefabs/weapons/sword/effects/hit-soft.prefab",
            "assets/prefabs/weapons/sword/effects/hit.prefab",
            "assets/prefabs/weapons/sword/effects/throw.prefab",
            "assets/prefabs/weapons/thompson/effects/attack.prefab",
            "assets/prefabs/weapons/thompson/effects/attack_muzzlebrake.prefab",
            "assets/prefabs/weapons/thompson/effects/attack_shake.prefab",
            "assets/prefabs/weapons/thompson/effects/attack_shake_ads.prefab",
            "assets/prefabs/weapons/thompson/effects/attack_silenced.prefab",
            "assets/prefabs/weapons/thompson/effects/bolt_action.prefab",
            "assets/prefabs/weapons/thompson/effects/deploy.prefab",
            "assets/prefabs/weapons/thompson/effects/dryfire.prefab",
            "assets/prefabs/weapons/thompson/effects/eject_pistol_shell.prefab",
            "assets/prefabs/weapons/thompson/effects/holster.prefab",
            "assets/prefabs/weapons/thompson/effects/idle_finger_taps.prefab",
            "assets/prefabs/weapons/thompson/effects/insert_clip.prefab",
            "assets/prefabs/weapons/thompson/effects/reload_begin.prefab",
            "assets/prefabs/weapons/thompson/effects/remove_clip.prefab",
            "assets/prefabs/weapons/thompson/effects/safety_off.prefab",
            "assets/prefabs/weapons/thompson/effects/w_drop_magazine.prefab",
            "assets/prefabs/weapons/thompson/effects/w_eject_pistol_shell.prefab",
            "assets/prefabs/weapons/toolgun/effects/attack.prefab",
            "assets/prefabs/weapons/toolgun/effects/lineeffect.prefab",
            "assets/prefabs/weapons/toolgun/effects/lineeffect_realistic.prefab",
            "assets/prefabs/weapons/toolgun/effects/repairerror.prefab",
            "assets/prefabs/weapons/toolgun/effects/ringeffect.prefab",
            "assets/prefabs/weapons/toolgun/effects/ringeffect_realistic.prefab",
            "assets/prefabs/weapons/torch/effects/attack.prefab",
            "assets/prefabs/weapons/torch/effects/attack_lit.prefab",
            "assets/prefabs/weapons/torch/effects/deploy.prefab",
            "assets/prefabs/weapons/torch/effects/extinguish.prefab",
            "assets/prefabs/weapons/torch/effects/ignite.prefab",
            "assets/prefabs/weapons/torch/effects/strike.prefab",
            "assets/prefabs/weapons/torch/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/torch/effects/torch_loop.prefab",
            "assets/prefabs/weapons/waterbucket/effects/deploy.prefab",
            "assets/prefabs/weapons/waterbucket/effects/fillbucket_fromcontainer.prefab",
            "assets/prefabs/weapons/waterbucket/effects/fillbucket_fromworld.prefab",
            "assets/prefabs/weapons/waterbucket/effects/waterbucket_splash.prefab",
            "assets/prefabs/weapons/waterbucket/effects/waterimpact_explosion.prefab",
            "assets/prefabs/weapons/waterbucket/effects/waterthrow.prefab",
            "assets/prefabs/weapons/waterbucket/effects/waterthrow3p.prefab",
            "assets/prefabs/weapons/wooden spear/effects/2hand_deploy.prefab",
            "assets/prefabs/weapons/wooden spear/effects/attack.prefab",
            "assets/prefabs/weapons/wooden spear/effects/deploy.prefab",
            "assets/prefabs/weapons/wooden spear/effects/holster.prefab",
            "assets/prefabs/weapons/wooden spear/effects/pull_out.prefab",
            "assets/prefabs/weapons/wooden spear/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/wooden spear/effects/strike_wood-muted.prefab",
            "assets/prefabs/weapons/wooden spear/effects/strike_wood-soft.prefab",
            "assets/prefabs/weapons/wooden spear/effects/strike_wood.prefab",
            "assets/prefabs/weapons/wooden spear/effects/throw.prefab",
            "assets/rust.ai/nextai/effects/dusttrail.prefab",
            "assets/standard assets/third party/camelotvfx_adv_water_fx/prefabs/splash_v3.prefab",
            "assets/standard assets/third party/detailed_pyro_fx/prefabs/4096/smoke_04.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion1.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion10.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion11.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion12.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion13.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion2.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_1.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_10.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_11.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_12.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_13.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_2.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_3.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_4.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_5.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_6.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_7.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_8.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion3d_9.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion4.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion5.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion6.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion7.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion8.prefab",
            "assets/third party/kriptofx/explosions/prefabs/mobile/mobileexplosion9.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion1.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion10.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion11.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion12.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion13.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion2.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_1.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_10.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_11.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_12.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_13.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_2.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_3.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_4.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_5.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_6.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_7.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_8.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion3d_9.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion4.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion5.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion6.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion7.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion8.prefab",
            "assets/third party/kriptofx/explosions/prefabs/pc/explosion9.prefab",
        };

        #endregion

        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            PrintWarning("" +
                "\n===================== Автор : Mercury" +
                "\n===================== Моя группа с разработкой плагинов - https://vk.com/mercurydev" +
                "\n===================== Мой ВК - https://vk.com/mir_inc" +
                $"\n===================== Иконок - {IconsRust.Count}" +
                $"\n===================== Материалов - {Materials.Count}" +
                $"\n===================== Шрифтов - {Fonts.Count}" +
                $"\n===================== Эффектов - {EffectRustList.Count}");
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (!HexTakePlayer.ContainsKey(player.userID))
                HexTakePlayer.Add(player.userID, "#FFFFFFFF");
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, PARENT_UI);
                CuiHelper.DestroyUi(player, PARENT_UI_HEX_SETTINGS);
                DestroyedLayer(player);
            }
        }
        #endregion

        #region Func Command

        [ConsoleCommand("utilites")] 
        void MercuryUtilitesCommands(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            string Key = arg.Args[0].ToLower();
            switch (Key)
            {
                case "icons":
                    {
                        string Hex = HexTakePlayer[player.userID];
                        int Page = Convert.ToInt32(arg.Args[1]);
                        UI_IconsLoaded(player, 0, Hex);
                        break;
                    }
                case "materials":
                    {
                        string Hex = HexTakePlayer[player.userID];
                        int Page = Convert.ToInt32(arg.Args[1]);
                        UI_MaterialLoaded(player, Page, Hex);
                        break;
                    }
                case "iconstwo":
                    {
                        string Hex = HexTakePlayer[player.userID];
                        int Page = Convert.ToInt32(arg.Args[1]);
                        UI_IconsLoadedMaterial(player, 0, Hex);
                        break;
                    }
                case "fonts":
                    {
                        string Hex = HexTakePlayer[player.userID];
                        int Page = Convert.ToInt32(arg.Args[1]);
                        UI_FontsLoaded(player, 0, Hex);
                        break;
                    }         
                case "effect":
                    {
                        int Page = Convert.ToInt32(arg.Args[1]);
                        UI_EffectLoaded(player, 0);
                        break;
                    }
                case "page_icons":
                    {
                        string PageAction = arg.Args[1];
                        int Page = Convert.ToInt32(arg.Args[2]);
                        switch (PageAction)
                        {
                            case "next":
                                {
                                    string Hex = HexTakePlayer[player.userID];

                                    UI_IconsLoaded(player, Page + 1, Hex);
                                    break;
                                }
                            case "back":
                                {
                                    string Hex = HexTakePlayer[player.userID];

                                    UI_IconsLoaded(player, Page - 1, Hex);
                                    break;
                                }
                        }
                        break;
                    }
                case "page_icons_two":
                    {
                        string PageAction = arg.Args[1];
                        int Page = Convert.ToInt32(arg.Args[2]);
                        switch (PageAction)
                        {
                            case "next":
                                {
                                    string Hex = HexTakePlayer[player.userID];

                                    UI_IconsLoadedMaterial(player, Page + 1, Hex);
                                    break;
                                }
                            case "back":
                                {
                                    string Hex = HexTakePlayer[player.userID];

                                    UI_IconsLoadedMaterial(player, Page - 1, Hex);
                                    break;
                                }
                        }
                        break;
                    }
                case "page_materials":
                    {
                        string PageAction = arg.Args[1];
                        int Page = Convert.ToInt32(arg.Args[2]);
                        switch (PageAction)
                        {
                            case "next":
                                {
                                    string Hex = HexTakePlayer[player.userID];

                                    UI_MaterialLoaded(player, Page + 1, Hex);
                                    break;
                                }
                            case "back":
                                {
                                    string Hex = HexTakePlayer[player.userID];

                                    UI_MaterialLoaded(player, Page - 1, Hex);
                                    break;
                                }
                        }
                        break;
                    }
                case "page_effect":
                    {
                        string PageAction = arg.Args[1];
                        int Page = Convert.ToInt32(arg.Args[2]);
                        switch (PageAction)
                        {
                            case "next":
                                {
                                    UI_EffectLoaded(player, Page + 1);
                                    break;
                                }
                            case "back":
                                {
                                    UI_EffectLoaded(player, Page - 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "show_hex": 
                    {
                        UI_HexSettingsMenu(player);
                        break;
                    }
                case "set_hex": 
                    {
                        string Hex = arg.Args[1];
                        HexTakePlayer[player.userID] = Hex;
                        CuiHelper.DestroyUi(player, PARENT_UI_HEX_SETTINGS);
                        PrintToChat($"Успешно установлен цвет {Hex}");
                        PrintToConsole($"Успешно установлен цвет {Hex}");
                        Puts($"Успешно установлен цвет {Hex}");
                        break;
                    }
                case "save_element": 
                    {
                        string Path = arg.Args[1];
                        PrintWarning(Path);
                        PrintToConsole(Path);
                        PrintToChat(Path);
                        break;
                    }
                case "sound_play":
                    {
                        string Path = arg.Args[1];
                        string Title = arg.Args[2];
                        int Page = Convert.ToInt32(arg.Args[3]);
                        CuiHelper.DestroyUi(player, PARENT_UI);
                        UI_Plaeer(player, Title,Path, Page);
                        break;
                    }
            }
        }

        [ChatCommand("ut")]
        void ChatCommandUtilites(BasePlayer player)
        {
            UI_PanelReportsPlayer(player);
        }

        [ConsoleCommand("ut")]
        void ConsoleCommandUtilMer(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            UI_PanelReportsPlayer(player);

        }

        #endregion

        #region UI
        public static string PARENT_UI = "MERCURY_PANEL_UI";
        public static string PARENT_UI_BUTTON = "MERCURY_PARENT_UI_BUTTON";

        public static string PARENT_UI_ELEMENT = "MERCURY_PARENT_UI_ELEMENT";
        public static string PARENT_UI_ELEMENT_ICONS = "PARENT_UI_ELEMENT_ICONS";
        public static string PARENT_UI_ELEMENT_ICONSTWO = "PARENT_UI_ELEMENT_ICONSTWO";

        public static string PARENT_UI_ELEMENT_MATERIAL = "PARENT_UI_ELEMENT_MATERIAL";

        public static string PARENT_UI_ELEMENT_FONTS = "PARENT_UI_ELEMENT_FONTS";

        public static string PARENT_UI_ELEMENT_EFFECT = "PARENT_UI_ELEMENT_EFFECT";
        public static string PARENT_UI_ELEMENT_EFFECT_PLAYER = "PARENT_UI_ELEMENT_EFFECT_PLAYER";

        public static string PARENT_UI_HEX_SETTINGS = "PARENT_UI_HEX_SETTINGS";

        #region MainPanel
        void UI_PanelReportsPlayer(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_UI);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"},
                Image = { Color = HexToRustFormat("#21211AF2"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            },  "Overlay",PARENT_UI);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.8407407", AnchorMax = "1 0.9268518" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_UI, PARENT_UI_BUTTON);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9305556", AnchorMax = "1 1" },
                Text = { Text = $"<b><size=30>MERCURY UTILITES</size></b>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI);

            #region BTNS
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.1671875 1" },
                Button = { Command = $"utilites icons {0}", Color = HexToRustFormat("#3E482EFF") },
                Text = { Text = "<b><size=20>ИКОНКИ ИЗ ИГРЫ</size></b>", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_BUTTON);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1718755 0", AnchorMax = "0.3390611 1" },
                Button = { Command = $"utilites materials {0}", Color = HexToRustFormat("#3E482EFF") },
                Text = { Text = "<b><size=20>МАТЕРИАЛЫ ИЗ ИГРЫ</size></b>", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_BUTTON);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3442709 0", AnchorMax = "0.5114543 1" },
                Button = { Command = $"utilites iconstwo {0}", Color = HexToRustFormat("#3E482EFF") },
                Text = { Text = "<b><size=20>ИКОНКИ ИЗ ИГРЫ С МАТЕРИАЛОМ</size></b>", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_BUTTON);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5156225 0", AnchorMax = "0.6828059 1" },
                Button = { Command = $"utilites fonts {0}", Color = HexToRustFormat("#3E482EFF") },
                Text = { Text = "<b><size=20>ШРИФТЫ</size></b>", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_BUTTON);  

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.6890574 0", AnchorMax = "0.8562407 1" },
                Button = { Command = $"utilites effect {0}", Color = HexToRustFormat("#3E482EFF") },
                Text = { Text = "<b><size=20>ЭФФЕКТЫ</size></b>", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_BUTTON);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.8" },
                Text = { Text = $"<b>Автор : Mercury\n\nМоя группа с разработкой плагинов - https://vk.com/mercurydev \n\nМой ВК - https://vk.com/mir_inc </b>", Font = "robotocondensed-bold.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter }
            },  PARENT_UI, "WELCOME_TITLE");
            #endregion

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.8314815" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_UI, PARENT_UI_ELEMENT);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.004687482 0.01113585", AnchorMax = "0.15 0.06347439" },
                Button = { Close = PARENT_UI, Color = HexToRustFormat("#B4371EFF") },
                Text = { Text = "<b><size=16>ЗАКРЫТЬ</size></b>", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_ELEMENT);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1583337 0.01113585", AnchorMax = "0.303645 0.06347439" },
                Button = { Command = "utilites show_hex", Color = HexToRustFormat("#3E482EFF") },
                Text = { Text = "<b><size=16>СМЕНИТЬ ЦВЕТ</size></b>", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_ELEMENT);


            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Icons
        
        void UI_IconsLoaded(BasePlayer player, int Page = 0, string Hex = "#FFFFFFFF")
        {
            CuiElementContainer container = new CuiElementContainer();
            DestroyedLayer(player);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.07683742", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_UI_ELEMENT, PARENT_UI_ELEMENT_ICONS);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5015628 0.004454346", AnchorMax = "0.5312498 0.06681515" },
                Text = { Text = $"<size=30>{Page}</size>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_ELEMENT, "PAGE_TITLE");

            if ((IconsRust.Count - (Page * 199)) > 199)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5385414 0.004454346", AnchorMax = "0.5682284 0.07015589" },
                    Button = { Command = $"utilites page_icons next {Page}", Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "<b><size=20>></size></b>", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI_ELEMENT, "PAGE_NEXT");
            }

            if (Page > 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4677091 0.004454346", AnchorMax = "0.4973962 0.07015589" },
                    Button = { Command = $"utilites page_icons back {Page}", Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "<b><size=20><</size></b>", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI_ELEMENT, "PAGE_BACK");
            }

            int i = 0, x = 0, y = 0;
            foreach (var Sprite in IconsRust.Skip(Page * 199))
            {
                container.Add(new CuiElement
                {
                    Name = $"ICON_{i}",
                    Parent = PARENT_UI_ELEMENT_ICONS,
                    Components =
                    {
                        new CuiRawImageComponent {  Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = $"{0.004687482 + (x * 0.05)} {0.9071308 - (y * 0.1)}", AnchorMax = $"{0.04218748 + (x * 0.05)} {0.9936692 - (y * 0.1)}" },
                        new CuiOutlineComponent { Color = HexToRustFormat("#3E482EFF") ,Distance = "0.2 -0.2" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = $"ICON_{i}",
                    Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Hex), Sprite = Sprite },
                        new CuiRectTransformComponent { AnchorMin = $"0.05555556 0.05555553", AnchorMax = $"0.9444445 0.9444441" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"utilites save_element {Sprite}", Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                },  $"ICON_{i}");

                i++;
                x++;
                if(x == 20)
                {
                    x = 0;
                    y++;
                }
                if (x == 0 && y == 10) break;
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Icons#2

        void UI_IconsLoadedMaterial(BasePlayer player, int Page = 0, string Hex = "#FFFFFFFF")
        {
            CuiElementContainer container = new CuiElementContainer();
            DestroyedLayer(player);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.07683742", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_UI_ELEMENT, PARENT_UI_ELEMENT_ICONSTWO);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5015628 0.004454346", AnchorMax = "0.5312498 0.06681515" },
                Text = { Text = $"<size=30>{Page}</size>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_ELEMENT, "PAGE_TITLE");

            if ((IconsRust.Count - (Page * 199)) > 199)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5385414 0.004454346", AnchorMax = "0.5682284 0.07015589" },
                    Button = { Command = $"utilites page_icons_two next {Page}", Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "<b><size=20>></size></b>", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI_ELEMENT, "PAGE_NEXT");
            }

            if (Page > 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4677091 0.004454346", AnchorMax = "0.4973962 0.07015589" },
                    Button = { Command = $"utilites page_icons_two back {Page}", Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "<b><size=20><</size></b>", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI_ELEMENT, "PAGE_BACK");
            }

            int i = 0, x = 0, y = 0;
            foreach (var Sprite in IconsRust.Skip(Page * 199))
            {
                container.Add(new CuiElement
                {
                    Name = $"ICON_{i}",
                    Parent = PARENT_UI_ELEMENT_ICONSTWO,
                    Components =
                    {
                        new CuiRawImageComponent {  Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = $"{0.004687482 + (x * 0.05)} {0.9071308 - (y * 0.1)}", AnchorMax = $"{0.04218748 + (x * 0.05)} {0.9936692 - (y * 0.1)}" },
                        new CuiOutlineComponent { Color = HexToRustFormat("#3E482EFF") ,Distance = "0.2 -0.2" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = $"ICON_{i}",
                    Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Hex), Sprite = Sprite, Material = Sprite },
                        new CuiRectTransformComponent { AnchorMin = $"0.05555556 0.05555553", AnchorMax = $"0.9444445 0.9444441" }
                    }
                });

                string Out = $"В данном случае используйте материал и спрайт для элемента одновременно :\n {Sprite}";
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"utilites save_element {Out}", Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, $"ICON_{i}");

                i++;
                x++;
                if (x == 20)
                {
                    x = 0;
                    y++;
                }
                if (x == 0 && y == 10) break;
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Materials
        void UI_MaterialLoaded(BasePlayer player, int Page = 0, string Hex = "#FFFFFFFF")
        {
            CuiElementContainer container = new CuiElementContainer();
            DestroyedLayer(player);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.07683742", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_UI_ELEMENT, PARENT_UI_ELEMENT_MATERIAL);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5015628 0.004454346", AnchorMax = "0.5312498 0.06681515" },
                Text = { Text = $"<size=30>{Page}</size>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_ELEMENT, "PAGE_TITLE");

            if ((Materials.Count - (Page * 45)) > 45)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5385414 0.004454346", AnchorMax = "0.5682284 0.07015589" },
                    Button = { Command = $"utilites page_materials next {Page}", Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "<b><size=20>></size></b>", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI_ELEMENT, "PAGE_NEXT");
            }

            if (Page > 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4677091 0.004454346", AnchorMax = "0.4973962 0.07015589" },
                    Button = { Command = $"utilites page_materials back {Page}", Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "<b><size=20><</size></b>", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI_ELEMENT, "PAGE_BACK");
            }

            int i = 0, x = 0, y = 0;
            foreach (var Material in Materials.Skip(Page * 45))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.004166649 + (x * 0.206)} {0.9083153 - (y * 0.1)}", AnchorMax = $"{0.1666667 + (x * 0.206)} {0.9951669 - (y * 0.1)}" },
                    Image = { Color = HexToRustFormat(Hex), Material = Material }
                }, PARENT_UI_ELEMENT_MATERIAL, $"MATERIAL_{i}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"utilites save_element {Material}", Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, $"MATERIAL_{i}");

                i++;
                x++;
                if (x == 5)
                {
                    x = 0;
                    y++;
                }
                if (x == 0 && y == 10) break;
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Fonts
        void UI_FontsLoaded(BasePlayer player, int Page = 0, string Hex = "#FFFFFFFF")
        {
            CuiElementContainer container = new CuiElementContainer();
            DestroyedLayer(player);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.07683742", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_UI_ELEMENT, PARENT_UI_ELEMENT_FONTS);


            int i = 0, x = 0, y = 0;
            foreach (var FontUse in Fonts)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.004166649 + (x * 0.254)} {0.9083153 - (y * 0.1)}", AnchorMax = $"{0.2291667 + (x * 0.254)} {0.9951669 - (y * 0.1)}" },
                    Image = { Color = HexToRustFormat("#3E482EFF") }
                }, PARENT_UI_ELEMENT_FONTS, $"FONT_{i}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"utilites save_element {FontUse}", Color = "0 0 0 0" },
                    Text = { Text = "Пример текста 12345", Font = FontUse.Replace("assets/content/ui/fonts/",""), Color = HexToRustFormat(Hex), FontSize = 18, Align = TextAnchor.MiddleCenter }
                }, $"FONT_{i}");

                i++;
                x++;
                if (x == 4)
                {
                    x = 0;
                    y++;
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Effects 
        void UI_EffectLoaded(BasePlayer player, int Page = 0)
        {
            CuiElementContainer container = new CuiElementContainer();
            DestroyedLayer(player);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.07683742", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_UI_ELEMENT, PARENT_UI_ELEMENT_EFFECT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5015628 0.004454346", AnchorMax = "0.5312498 0.06681515" },
                Text = { Text = $"<size=30>{Page}</size>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_ELEMENT, "PAGE_TITLE");

            if ((EffectRustList.Count - (Page * 50)) > 50)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5385414 0.004454346", AnchorMax = "0.5682284 0.07015589" },
                    Button = { Command = $"utilites page_effect next {Page}", Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "<b><size=20>></size></b>", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI_ELEMENT, "PAGE_NEXT");
            }

            if (Page > 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4677091 0.004454346", AnchorMax = "0.4973962 0.07015589" },
                    Button = { Command = $"utilites page_effect back {Page}", Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "<b><size=20><</size></b>", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI_ELEMENT, "PAGE_BACK");
            }

            int i = Page * 50, x = 0, y = 0;
            foreach (var Effect in EffectRustList.Skip(Page * 50))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.004166649 + (x * 0.204)} {0.9083153 - (y * 0.1)}", AnchorMax = $"{0.1765625 + (x * 0.204)} {0.9951669 - (y * 0.1)}" },
                    Image = { Color = HexToRustFormat("#3E482EFF") }
                }, PARENT_UI_ELEMENT_EFFECT, $"EFFECT_{i}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.7522659 1" },
                    Text = { Text = $"<size=18><b>ЭФФЕКТ #{i}</b></size>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                },  $"EFFECT_{i}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"utilites save_element {Effect}", Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, $"EFFECT_{i}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.7824773 0.06555558", AnchorMax = "0.9788519 0.9167581" },
                    Button = { Command = $"utilites sound_play {Effect} <size=25><b>ЭФФЕКТ#{i}</b></size> {Page}", Color = HexToRustFormat("#06C4FFFF"), Sprite = "assets/icons/voice.png" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, $"EFFECT_{i}");

                i++;
                x++;
                if (x == 5)
                {
                    x = 0;
                    y++;
                }
                if (x == 0 && y == 10) break;
            }

            CuiHelper.AddUi(player, container);
        }

        #region UI Pleer

        void UI_Plaeer(BasePlayer player, string Title, string Path, int Page = 0)
        {
            CuiElementContainer container = new CuiElementContainer();
            DestroyedLayer(player);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.6907408", AnchorMax = "0.1859375 0.7444444" },
                Image = { Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            },  "Overlay", PARENT_UI_ELEMENT_EFFECT_PLAYER);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.7754629 1" },
                Text = { Text = $"<size=20><b>{Title}</b></size>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_ELEMENT_EFFECT_PLAYER);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.8055555 0.06555558", AnchorMax = "0.9742223 0.9167581" },
                Image = { Color = HexToRustFormat("#04C4FFFF"), Sprite = "assets/icons/voice.png" }
            }, PARENT_UI_ELEMENT_EFFECT_PLAYER);

            RunEffect(player, Path);
            timer.Once(2f, () => {
                UI_PanelReportsPlayer(player);
                UI_EffectLoaded(player, Page);
            });

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #endregion

        #region HexSettings
        void UI_HexSettingsMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_UI_HEX_SETTINGS);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.2744792 0.06666666", AnchorMax = "0.725 0.8268518" },
                Image = { Color = HexToRustFormat("#54514DFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            },  "Overlay", PARENT_UI_HEX_SETTINGS);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9159561", AnchorMax = "1 1" },
                Text = { Text = $"<b><size=20>ВЫБЕРИТЕ ЦВЕТ ДЛЯ ПРЕДПРОСМОТРА</size></b>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  PARENT_UI_HEX_SETTINGS);

            int x = 0, y = 0;
            for (int i = 0; i < HexList.Count; i++)
            {
                string Hex = HexList[i];

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0.02312142 + (x * 0.5)} {0.8343483 - (y * 0.1)}", AnchorMax = $"{0.4913295 + (x * 0.5)} {0.9001219 - (y * 0.1)}" },
                    Button = { Command = $"utilites set_hex {Hex}", Close = PARENT_UI_HEX_SETTINGS, Color = HexToRustFormat(Hex) },
                    Text = { Text = $"<b><size=16>{Hex}</size></b>", Align = TextAnchor.MiddleCenter }
                },  PARENT_UI_HEX_SETTINGS, $"BTN_HEX_{i}");

                x++;
                if(x == 2)
                {
                    x = 0;
                    y++;
                }
                if (x == 0 && y == 8) break;
            }

            string CustomHex = "";
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.02658963 0.08404718", AnchorMax = "0.9815028 0.1327649" },
                Text = { Text = $"<b><size=20>ВВЕДИТЕ СОБСТВЕННЫЙ HEX</size></b>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, PARENT_UI_HEX_SETTINGS);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.02312142 0.01340157", AnchorMax = "0.9780346 0.07917558" },
                Image = { Color = HexToRustFormat("#3E482EFF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, PARENT_UI_HEX_SETTINGS, PARENT_UI_HEX_SETTINGS + ".Input");

            container.Add(new CuiElement
            {
                Parent = PARENT_UI_HEX_SETTINGS + ".Input",
                Name = PARENT_UI_HEX_SETTINGS + ".Input.Current",
                Components =
                {
                    new CuiInputFieldComponent { Text = CustomHex, FontSize = 18,Command = $"utilites set_hex {CustomHex}", Align = TextAnchor.MiddleCenter, CharsLimit = 10},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion

        void RunEffect(BasePlayer player, string Path)
        {
            Effect effect = new Effect(Path, player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);
        }

        void DestroyedLayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PARENT_UI_ELEMENT_ICONS);
            CuiHelper.DestroyUi(player, PARENT_UI_ELEMENT_ICONSTWO);
            CuiHelper.DestroyUi(player, PARENT_UI_ELEMENT_MATERIAL);
            CuiHelper.DestroyUi(player, PARENT_UI_ELEMENT_EFFECT);
            CuiHelper.DestroyUi(player, PARENT_UI_ELEMENT_FONTS);
            CuiHelper.DestroyUi(player, PARENT_UI_ELEMENT_EFFECT_PLAYER);
            CuiHelper.DestroyUi(player, "WORK_PANEL");
            CuiHelper.DestroyUi(player, "PAGE_TITLE");
            CuiHelper.DestroyUi(player, "PAGE_NEXT");
            CuiHelper.DestroyUi(player, "WELCOME_TITLE");
            CuiHelper.DestroyUi(player, "PAGE_BACK");
        }

        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion
    }
}
