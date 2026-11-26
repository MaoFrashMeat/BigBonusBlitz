import unreal,time,pathlib,json,traceback
out=pathlib.Path(__file__).resolve().parent
out.mkdir(parents=True,exist_ok=True)
phase="boot"
start=time.monotonic()
games=0
def finish(note):
    (out/"capture.json").write_text(json.dumps({"result":note,"games":games},ensure_ascii=False),encoding="utf8")
    unreal.unregister_slate_post_tick_callback(handle)
    unreal.SystemLibrary.execute_console_command(unreal.EditorLevelLibrary.get_editor_world(),"QUIT_EDITOR")
def tick(dt):
    global phase,world,ctrl,games,wait
    try:
        if time.monotonic()-start>220:return finish("timeout "+phase)
        if phase=="boot":
            unreal.get_editor_subsystem(unreal.LevelEditorSubsystem).editor_request_begin_play()
            phase="world"
        elif phase=="world":
            worlds=unreal.EditorLevelLibrary.get_pie_worlds(False)
            if not worlds:return
            world=worlds[0]
            actors=unreal.GameplayStatics.get_all_actors_of_class(world,unreal.BBBMainStageController)
            if not actors:return
            ctrl=actors[0]
            ctrl.start_new_game()
            phase="spin"
        elif phase=="spin":
            for i in range(8):
                ctrl.choose_curse(False)
                ctrl.action_lever()
                for r in range(3):ctrl.action_stop_reel(r)
                games+=1
                if games==25:
                    ctrl.save_game_now()
                    savepath=pathlib.Path(unreal.Paths.project_saved_dir())/"SaveGames/bbb_save.json"
                    original=json.loads(savepath.read_text(encoding="utf-8-sig"))
                    assert original["totalSpins"]>0 and "coreProgress" in original
                    ctrl.start_new_game()
                    ctrl.continue_game()
                    ctrl.save_game_now()
                    after=json.loads(savepath.read_text(encoding="utf-8-sig"))
                    differences={k:[v,after.get(k)] for k,v in original.items() if v!=after.get(k)}
                    (out/"roundtrip.json").write_text(json.dumps(differences,ensure_ascii=False),encoding="utf8")
                    assert not differences,differences
                    ctrl.choose_curse(False)
                    ctrl.action_lever()
                    for r in (2,1,0):ctrl.action_stop_reel(r)
                    phase="capture"
                    wait=time.monotonic()+1
                    break
        elif phase=="capture" and time.monotonic()>wait:
            cfg=json.loads((pathlib.Path(unreal.Paths.project_dir())/"Data/Json/game_config.json").read_text(encoding="utf-8-sig"))
            path=pathlib.Path(unreal.Paths.project_saved_dir())/"SaveGames/bbb_save.json"
            ctrl.save_game_now()
            state=json.loads(path.read_text(encoding="utf-8-sig"))
            state["souls"]=50000
            state["unspentPoints"]=5
            recipe=next(r for r in cfg["craft"]["recipes"] if not r.get("unlockChapter",0))
            state["coreProgress"]["materials"]={k:999 for k in recipe["materialIds"]}
            path.write_text(json.dumps(state),encoding="utf8")
            ctrl.continue_game()
            ctrl.craft_item(recipe["id"])
            ctrl.allocate_stat("luck")
            ctrl.buy_shop_item(cfg["shop"]["items"][0]["id"])
            ctrl.save_game_now()
            crafted=json.loads(path.read_text(encoding="utf-8-sig"))
            assert crafted["statLuck"]==state["statLuck"]+1 and crafted["unspentPoints"]==4
            assert crafted["shopOwned"] and crafted["souls"]<50000
            assert any(it.get("craftId")==recipe["id"] for it in list(crafted["coreProgress"]["worn"].values())+crafted["coreProgress"]["bag"])
            ctrl.start_new_game()
            ctrl.continue_game()
            ctrl.save_game_now()
            restored=json.loads(path.read_text(encoding="utf-8-sig"))
            assert restored==crafted
            ctrl.open_sub_screen(unreal.BBBSubScreen.WORKSHOP)
            phase="workshop"
            wait=time.monotonic()+1
        elif phase in ("workshop","achievements","curse") and time.monotonic()>wait:
            unreal.SystemLibrary.execute_console_command(world,'shot showui -nosuffix filename="'+str(out/(phase+".png"))+'"')
            phase="after_"+phase
            wait=time.monotonic()+1
        elif phase=="after_workshop" and time.monotonic()>wait:
            ctrl.open_sub_screen(unreal.BBBSubScreen.ACHIEVEMENTS)
            phase="achievements"
            wait=time.monotonic()+1
        elif phase=="after_achievements" and time.monotonic()>wait:
            path=pathlib.Path(unreal.Paths.project_saved_dir())/"SaveGames/bbb_save.json"
            state=json.loads(path.read_text(encoding="utf-8-sig"))
            state["coreProgress"]["offer"]={"curseId":"qa","curseName":"呪いの試練","curseEffect":"betExtra","curseValue":1,"blessId":"qa","blessName":"冒険の祝福","blessEffect":"judgeBonus","blessValue":5,"depth":1}
            path.write_text(json.dumps(state),encoding="utf8")
            ctrl.continue_game()
            phase="curse"
            wait=time.monotonic()+1
        elif phase=="after_curse" and time.monotonic()>wait:
            ctrl.choose_curse(True)
            ctrl.save_game_now()
            path=pathlib.Path(unreal.Paths.project_saved_dir())/"SaveGames/bbb_save.json"
            state=json.loads(path.read_text(encoding="utf-8-sig"))
            assert state["coreProgress"]["taken"][-1]["curseId"]=="qa"
            assert "offer" not in state["coreProgress"]
            state["credit"]=4
            state["coreProgress"]["torches"]=1
            state["coreProgress"]["torchSpins"]=1
            path.write_text(json.dumps(state),encoding="utf8")
            ctrl.continue_game()
            for attempt in range(50):
                ctrl.action_lever()
                for reel in range(3): ctrl.action_stop_reel(reel)
                ctrl.save_game_now()
                town=json.loads(path.read_text(encoding="utf-8-sig"))
                if not town["coreProgress"]["taken"]: break
            assert town["credit"]>=3 and not town["coreProgress"]["taken"],town
            ctrl.open_sub_screen(unreal.BBBSubScreen.NONE)
            ctrl.action_lever()
            for reel in range(3): ctrl.action_stop_reel(reel)
            ctrl.save_game_now()
            resumed=json.loads(path.read_text(encoding="utf-8-sig"))
            assert resumed["coreProgress"]["achievementCounters"]["spins"]==town["coreProgress"]["achievementCounters"]["spins"]+1
            finish("passed")
    except Exception:return finish(traceback.format_exc())
handle=unreal.register_slate_post_tick_callback(tick)



