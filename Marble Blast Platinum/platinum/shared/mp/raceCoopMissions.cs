//-----------------------------------------------------------------------------
// Race Co-op Missions
//
// Adds "Race" categories to the multiplayer mission list - one per mod
// (Gold, Platinum, ...) - built from local copies of the co-op mission set
// under $RaceCoop::BaseDirectory. Runs after the official list is fetched,
// so it never touches or replaces the live Marbleland-sourced data, only
// appends to it.
//
// Each category is its own top-level "game" entry (RaceGold, RacePlatinum,
// ...), mirroring how Co-op itself is already split into CoopGold/
// CoopPlatinum/etc rather than nested under a single "Co-op" entry. Every
// mission reached through one of these is forced into Race mode via
// force_gamemode, regardless of what the underlying .mis file was built for
// (the copied .mis files also carry gameMode = "race" directly, as a second
// belt-and-suspenders way of forcing it).
//
// USAGE: Add a row to $RaceCoop::Games for each mod to expose, pointing at
// a folder named "<mod>_<difficulty>" under $RaceCoop::BaseDirectory for
// each difficulty listed. Nothing else needs editing - the folders are the
// level list.
//-----------------------------------------------------------------------------

$RaceCoop::BaseDirectory = "platinum/data/multiplayer/race";
$RaceCoop::AddedFlag     = false; // guards against double-injection

// modId TAB display name TAB space-separated difficulty folder suffixes
$RaceCoop::Games[0] = "gold"     TAB "Race (Gold)"     TAB "beginner intermediate advanced";
$RaceCoop::Games[1] = "platinum" TAB "Race (Platinum)" TAB "beginner intermediate advanced expert";
$RaceCoop::Games[2] = "ultra"    TAB "Race (Ultra)"    TAB "beginner intermediate advanced";
$RaceCoop::GameCount = 3;

package RaceCoopMissions {

function statsGetMissionListChallengeLine(%line, %req) {
	// Let the real handler run first - this is what parses and stores the
	// live Marbleland categories, unmodified.
	Parent::statsGetMissionListChallengeLine(%line, %req);

	if (%req.gameType $= "Multiplayer" && !$RaceCoop::AddedFlag) {
		addRaceCoopCategories();
	}
}

};
activatePackage(RaceCoopMissions);

function addRaceCoopCategories() {
	%ml = getMissionList("mp");
	if (!isObject(%ml.onlineMissionList)) {
		error("addRaceCoopCategories: online mission list isn't ready yet");
		return;
	}

	%registered = 0;
	for (%g = 0; %g < $RaceCoop::GameCount; %g ++) {
		%modId        = getField($RaceCoop::Games[%g], 0);
		%display      = getField($RaceCoop::Games[%g], 1);
		%difficulties = getField($RaceCoop::Games[%g], 2);

		%gameId = "race" @ upperFirst(%modId);

		// Bail out quietly if the folders aren't there yet - lets these
		// scripts be dropped in even before the levels exist, without
		// erroring the server. (No isDirectory() in this engine build, so
		// check for an actual .mis file instead.)
		%probeDir = $RaceCoop::BaseDirectory @ "/" @ %modId @ "_" @ getWord(%difficulties, 0);
		if (findFirstMission(%probeDir @ "/*") $= "") {
			error("addRaceCoopCategories: no race levels found for" SPC %modId @ ", skipping");
			continue;
		}

		RootGroup.add(%difficultyArray = Array(RaceCoopDifficultyList @ %gameId));

		%dcount = getWordCount(%difficulties);
		for (%d = 0; %d < %dcount; %d ++) {
			%diffId  = getWord(%difficulties, %d);
			%dirName = $RaceCoop::BaseDirectory @ "/" @ %modId @ "_" @ %diffId;

			// "Difficulty" bucket - is_local means the engine scans the
			// directory directly for .mis files instead of expecting a
			// server-sent mission list
			RootGroup.add(%difficulty = new ScriptObject() {
				id                  = %gameId @ upperFirst(%diffId);
				name                = %diffId;
				display             = upperFirst(%diffId);
				directory           = %dirName;
				bitmap_directory    = %dirName;
				previews_directory  = %dirName;
				is_local            = true;
				game_id             = %gameId;
			});

			%difficultyArray.addEntry(%difficulty);
		}

		// "Game" bucket - this is what shows up as its own entry in the
		// multiplayer dropdown, and forces every mission under it into
		// Race mode
		RootGroup.add(%game = new ScriptObject() {
			id             = %gameId;
			name           = %gameId;
			display        = %display;
			force_gamemode = "race";
			has_blast      = (%modId $= "ultra");
			difficulties   = %difficultyArray;
		});

		// Append alongside the real, already-parsed Marbleland games
		%ml.onlineMissionList.games.addEntry(%game);
		%registered ++;
	}

	// Rebuild the lookup tables so the new games/difficulties are indexed -
	// this re-scans everything already in .games too, which is harmless
	%ml.buildMissionLookup();

	$RaceCoop::AddedFlag = true;

	echo("[RaceCoop]: Registered" SPC %registered SPC "of" SPC $RaceCoop::GameCount SPC "Race categories from" SPC $RaceCoop::BaseDirectory);
}