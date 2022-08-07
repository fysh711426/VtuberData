# Vtuber Data  

這個 repo 用於提供此網站的資料儲存。  

[https://fysh711426.github.io/vtuber/index.html](https://fysh711426.github.io/vtuber/index.html)  

---  

### 資料架構  

* Vtubers.csv：包含所有 Vtuber 的列表清單。  
* Data_{{datetime}}.csv：每日統計資訊按日期分類。  
* api：各分類的排名結果，只列前 100 名用於網站呈現。  

---  

### 更新時間  

每日 3:00 更新統計資訊，每 7 日更新 Vtuber 資訊。  

---  

### 使用方式  

首先使用 api 取得最新 commit 編號。  

[https://api.github.com/repos/fysh711426/VtuberData/commits/master](https://api.github.com/repos/fysh711426/VtuberData/commits/master)  

```json
"sha": "c09c9bda8f83a939f5bd62d96030f175608554dc"
```

接著透過 cdn 取得各類資訊。  

```html
https://cdn.statically.io/gh/{user}/{repo}/{commit}/{filepath}
```

```html
https://cdn.statically.io/gh/fysh711426/VtuberData/c09c9bda8f83a939f5bd62d96030f175608554dc/api/subscribe_tw.json
```



---  

### 欄位說明  

* Vtubers  

欄位 | 說明  
-----|------
Id | 編號
ChannelUrl | 頻道網址	
Status | 活動狀態
Name | 名稱
Area | 地區
Company	| 所屬公司
Group | 組別
ChannelName	| 頻道名稱
Thumbnail | 頻道縮圖
CreateTime | 創建時間
IsGreen | 用於綠屏

* Status  

欄位 | 說明  
-----|------
Prepare | 準備中
Activity | 活動中
NotActivity | 不積極活動
Graduate | 畢業
Closure | 手動排除

* Data  

欄位 | 說明  
-----|------
Id | 編號
ChannelUrl | 頻道網址	
Name | 名稱
SubscriberCount | 訂閱數	
ViewCount | 頻道觀看數
MedianViewCountDay7	| 近7天影片觀看中位數
HighestViewCountDay7 | 近7天影片最高觀看數
HighestViewVideoUrlDay7	| 近7天觀看數最高的影片網址
HighestViewVideoTitleDay7 | 近7天觀看數最高的影片標題
HighestViewVideoThumbnailDay7 | 近7天觀看數最高的影片縮圖
HighestSingingViewCountDay7	| 近7天歌回影片最高觀看數
HighestViewSingingVideoUrlDay7 | 近7天觀看數最高的歌回影片網址
HighestViewSingingVideoTitleDay7 | 近7天觀看數最高的歌回影片標題
HighestViewSingingVideoThumbnailDay7 | 近7天觀看數最高的歌回影片縮圖	
MedianViewCountDay30 | 近30天影片觀看中位數
HighestViewCountDay30 | 近30天影片最高觀看數
HighestViewVideoUrlDay30 | 近30天觀看數最高的影片網址
HighestViewVideoTitleDay30 | 近30天觀看數最高的影片標題
HighestViewVideoThumbnailDay30 | 近30天觀看數最高的影片縮圖
HighestSingingViewCountDay30 | 近30天歌回影片最高觀看數
HighestViewSingingVideoUrlDay30	| 近30天觀看數最高的歌回影片網址
HighestViewSingingVideoTitleDay30 | 近30天觀看數最高的歌回影片標題
HighestViewSingingVideoThumbnailDay30 | 近30天觀看數最高的歌回影片縮圖

* api  

欄位 | 說明  
-----|------
id | 編號
channelUrl | 頻道網址
name | 名稱
thumbnail | 頻道縮圖
subscribe | 訂閱數
score | 分數(各類排名依據)
videoTitle | 影片標題
videoViewCount | 影片觀看數
videoThumbnail | 影片縮圖
videoUrl | 影片網址
rank | 排名
rankVar | 排名變化

---  

### 專案細節  

更多專案細節請參考。  

[https://github.com/fysh711426/VtuberWeb](https://github.com/fysh711426/VtuberWeb)  

如果有幫助到你記得給我一顆星星，感謝大家。  