using UnityEngine;
using TMPro;
namespace Pigment {
public sealed class PigmentHud : MonoBehaviour {
    public PigmentGame game;
    public PigmentIcon[] resultStarIcons;
    public Canvas canvas;public RectTransform safeArea,header,targetTag,hand;
    public GameObject startPanel,resultPanel,celebrationPanel,guide;
    public TextMeshProUGUI targetName,levelLabel,matchLabel,hint,stars,percent,verdict,nextLabel,totalStars;
    public UnityEngine.UI.Image meter,want,got,tagDot;
    public UnityEngine.UI.Button play,retry,next,again;
    public UnityEngine.UI.CanvasScaler scaler;
    GamePhase previous=(GamePhase)(-1);Rect oldSafe;int oldWidth,oldHeight;
    void Awake(){play.onClick.AddListener(game.Begin);retry.onClick.AddListener(game.Retry);next.onClick.AddListener(()=>{if(game.Score>=game.rules.passPercent)game.Advance();else game.Retry();});again.onClick.AddListener(game.Again);}
    void PositionWorld(RectTransform rect,Vector3 world,Vector2 offset){Vector3 screen=game.gameCamera.WorldToScreenPoint(world);if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,null,out var p))rect.anchoredPosition=p+offset;}
    public void Refresh(PigmentGame g){
        bool portrait=Screen.width<Screen.height;
        if(oldWidth!=Screen.width||oldHeight!=Screen.height||oldSafe!=Screen.safeArea){oldWidth=Screen.width;oldHeight=Screen.height;oldSafe=Screen.safeArea;safeArea.anchorMin=new Vector2(oldSafe.x/Screen.width,oldSafe.y/Screen.height);safeArea.anchorMax=new Vector2(oldSafe.xMax/Screen.width,oldSafe.yMax/Screen.height);safeArea.offsetMin=safeArea.offsetMax=Vector2.zero;scaler.matchWidthOrHeight=portrait?0:1;header.anchorMin=header.anchorMax=header.pivot=portrait?new Vector2(.5f,1):new Vector2(0,1);header.anchoredPosition=new Vector2(portrait?0:14,-14);}
        bool intro=g.Phase==GamePhase.Intro,celebrate=g.Phase==GamePhase.Celebration,result=g.Phase==GamePhase.Result;
        startPanel.SetActive(intro);resultPanel.SetActive(result);celebrationPanel.SetActive(celebrate);header.gameObject.SetActive(!intro&&!celebrate);targetTag.gameObject.SetActive(!intro&&!celebrate);retry.interactable=g.Phase!=GamePhase.Entering;
        targetName.text=g.levels[g.LevelIndex].displayName;levelLabel.text="HEDEF RENK  ·  "+(g.LevelIndex+1)+"/"+g.levels.Length;meter.fillAmount=g.Fill;matchLabel.text=g.Fill>0?"%"+g.Score:"—";tagDot.color=g.levels[g.LevelIndex].TargetColor;
        hint.gameObject.SetActive(g.Phase==GamePhase.Playing&&g.Fill<.02f&&g.Active==null);
        if(g.Reference){PositionWorld(targetTag,g.Reference.transform.position+Vector3.up*(g.Reference.Height+.22f),Vector2.up*8);PositionWorld(hint.rectTransform,g.Reference.home-Vector3.up*.2f,Vector2.down*12);}
        bool teaching=g.style.tutorial&&g.LevelIndex==0&&g.Phase==GamePhase.Playing&&g.Fill<.02f&&!g.Held;guide.SetActive(teaching);
        if(teaching&&g.Sources.Count>0){var vessel=g.Active;if(!vessel){vessel=g.Sources[0];foreach(var s in g.Sources)if(g.gameCamera.WorldToViewportPoint(s.transform.position).x>g.gameCamera.WorldToViewportPoint(vessel.transform.position).x)vessel=s;}Vector3 p=vessel.transform.TransformPoint(Vector3.up*vessel.definition.height*.5f);PositionWorld((RectTransform)guide.transform,p,Vector2.zero);float wave=Mathf.Sin(Time.unscaledTime*Mathf.PI*2/g.style.pressDuration);hand.sizeDelta=new Vector2(g.style.handSize,g.style.handSize*420/349f);hand.localScale=Vector3.one*(.96f+wave*.035f);hand.anchoredPosition=new Vector2(-g.style.handSize*.455f,-g.style.handSize*.6f)+new Vector2(-4,-6)*(wave+1);}
        if(result){int count=g.rules.Stars(g.Score);stars.text="";if(resultStarIcons!=null)for(int i=0;i<resultStarIcons.Length;i++)resultStarIcons[i].color=i<count?new Color(1,.82f,.4f):new Color(.35f,.29f,.22f);percent.text="%"+g.Score;verdict.text=count==3?"Kusursuz karışım!":count==2?"Harika!":count==1?"Oldu!":"Biraz uzak kaldı";want.color=g.levels[g.LevelIndex].TargetColor;got.color=PigmentMath.MixColor(g.Mix);nextLabel.text=count>0?"Sonraki renk":"Tekrar dene";}
        if(celebrate){int sum=0;foreach(int s in g.BestStars)sum+=s;totalStars.text=sum.ToString()+" / "+g.levels.Length*3;}
        if(g.Phase!=previous){previous=g.Phase;}
    }
}
}
