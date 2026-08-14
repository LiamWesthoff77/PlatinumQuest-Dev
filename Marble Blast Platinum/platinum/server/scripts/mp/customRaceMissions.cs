//-----------------------------------------------------------------------------
// Custom Race Missions
//
// Adds a "Custom Race" category to the multiplayer mission list, built from a
// local folder of .mis files, alongside the normal Marbleland-sourced
// categories. Runs after the official list is fetched, so it never touches
// or replaces the live data - it only appends to it.
//
// USAGE: Drop copies (or symlinks) of any .mis file you want playable in
// Race mode into $CustomRace::Directory. Nothing else needs editing per file
// - the folder itself is the level list. Every level in it is forced into
// Race mode regardless of what mode it was originally built for.
//-----------------------------------------------------------------------------

$CustomRace::Directory = "platinum/data/missions/customRace";
$CustomRace::GameName  = "customRace";   // internal id, must be unique
$CustomRace::AddedFlag = false;          // guards against double-injection

package CustomRaceMissions {

function statsGetMissionListChallengeLine(%line, %req) {
	// Let the real handler run first - this is what parses and stores the
	// live Marbleland categories, unmodified.
	Parent::statsGetMissionListChallengeLine(%line, %req);

	if (%req.gameType $= "Multiplayer" && !$CustomRace::AddedFlag) {
		addCustomRaceCategory();
	}
}

};
activatePackage(CustomRaceMissions);

function addCustomRaceCategory() {
	%ml = getMissionList("mp");
	if (!isObject(%ml.onlineMissionList)) {
		error("addCustomRaceCategory: online mission list isn't ready yet");
		return;
	}

	// Bail out quietly if there's nothing to scan yet - lets you drop this
	// script in even before the folder exists without erroring the server.
	// (No isDirectory() in this engine build, so check for an actual .mis
	// file instead.)
	if (findFirstMission($CustomRace::Directory @ "/*") $= "") {
		error("addCustomRaceCategory: directory " @ $CustomRace::Directory @ " doesn't exist, skipping");
		return;
	}

	// "Difficulty" bucket - is_local means the engine scans the directory
	// directly for .mis files instead of expecting a server-sent mission list
	RootGroup.add(%difficulty = new ScriptObject() {
		id                  = "customRaceAll";
		name                = "All";
		display             = "All Levels";
		directory           = $CustomRace::Directory;
		bitmap_directory    = $CustomRace::Directory;
		previews_directory  = $CustomRace::Directory;
		is_local            = true;
	});

	RootGroup.add(%difficulties = Array(CustomRaceDifficultyList));
	%difficulties.addEntry(%difficulty);

	// "Game" bucket - this is what shows up as its own entry in the
	// multiplayer dropdown, and forces every mission under it into Race mode
	RootGroup.add(%game = new ScriptObject() {
		id             = "customRace";
		name           = $CustomRace::GameName;
		display        = "Custom Race";
		force_gamemode = "race";
		has_blast      = false;
		difficulties   = %difficulties;
	});

	// Append alongside the real, already-parsed Marbleland games
	%ml.onlineMissionList.games.addEntry(%game);

	// Rebuild the lookup tables so the new game/difficulty are indexed -
	// this re-scans everything already in .games too, which is harmless
	%ml.buildMissionLookup();

	$CustomRace::AddedFlag = true;

	echo("[CustomRace]: Registered Race category from " @ $CustomRace::Directory);
}
