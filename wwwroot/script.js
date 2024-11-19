var events = {
  '#log-out:click': Logout,
  '#search-pixabay:click': SearchPixabay,
  '#pixabay-results:scroll': ScrollPixabayResults,
  '.pixabay-result:dblclick': SaveImage,
  '#image-browse-select:change': SelectTag
}

Array.prototype.contains = function(str) {
  for (var i = 0; i < this.length; i++) {
    if (this[i] == str) {
      return true
    }
  }
  return false
}

var view = {
  dimension: 20
}

var control = {
  image_urls: {}
}

var player = {

}

function getCookieValue(cookieName) {
  const cookies = document.cookie.split(';'); // Split all cookies into an array
  for (let cookie of cookies) {
    cookie = cookie.trim(); // Remove leading/trailing whitespace
    if (cookie.startsWith(`${cookieName}=`)) {
        return cookie.substring(cookieName.length + 1); // Return the value
    }
  }
  return null; // Return null if the cookie is not found
}

function Logout() {
  console.log('logging out')
  var xhr = new XMLHttpRequest()
  xhr.open('POST', '/logout')
  xhr.addEventListener('load', function() {
    var res = JSON.parse(this.response)
    if (res.status == 'success') {
      location.reload()
    }
  })
  xhr.send()
} 

function AddEvents(selector, element) {
  for (var key in events) {
    var split_key = key.split(':')
    if (split_key[0] == selector) {
      document.querySelectorAll(split_key[0]).forEach(element => {
        element.addEventListener(split_key[1], events[key])
      })
    }
  }
}

function Pixabay(term, page) {
  return new Promise(resolve => {
    term = term.trim().split(' ').join('+')
    var image_type = $('#search-vector').is(':checked') ? 'vector' : 'all'
    var xhr = new XMLHttpRequest()
    xhr.open('GET', `https://pixabay.com/api/?key=25483695-93658ed46b8876fc2d6419379&q=${term}&per_page=25&image_type=${image_type}&page=${page}`)
    xhr.addEventListener('load', function() {
      resolve(JSON.parse(this.response))
    })
    xhr.send()
  })
}

function AppendImageDiv(results, image_url, tag, selector) {
  var div = document.createElement('div')
  div.classList.add('pixabay-result')
  div.style.background = `url(${image_url})`
  div.dataset.tag = tag
  MakeDraggable(div)
  AddEvents(selector, div)
  results.appendChild(div)
}

var searching = false
var urls = {}
function SearchPixabay(event, fresh = true) {
  if (searching) return
  searching = true
  try {
    var elem = document.getElementById('pixabay-search')
    var results = document.getElementById('pixabay-results')
    if (fresh) {
      results.innerHTML = ''
      urls = {}
    }
    var term = elem.value
    var page = elem.dataset.page
    Pixabay(term, page).then(json => {
      var hits = []
      for (var i = 0; i < json.hits.length; i++) {
        if (!urls[json.hits[i].largeImageURL]) {
          urls[json.hits[i].largeImageURL] = true
          hits.push(json.hits[i])
        }
      } 
      for (var i = 0; i < hits.length; i++) {
        AppendImageDiv(results, hits[i].largeImageURL, term, '.pixabay-result')
      }
      elem.dataset.page = +page + 1
      searching = false
    })
  } catch (err) {} finally { searching = false; }
}

function ScrollPixabayResults(event) {
  const scrollableElement = event.target;
  const isAtBottom = 
    scrollableElement.scrollHeight - scrollableElement.scrollTop <= scrollableElement.clientHeight;
  if (isAtBottom) {
    SearchPixabay(null, false);
  }
}

function ParseImageId(element) {
  return element.style.background.replace('url(', '').replace(')', '').replaceAll("'", '').replaceAll('"', '').split('/').pop()
}

function SaveImage(event) {
  var url = event.srcElement.style.background.replace('url(', '').replace(')', '').replaceAll("'", '').replaceAll('"', '')
  var xhr = new XMLHttpRequest()
  xhr.open('POST', '/save-image')
  xhr.addEventListener('load', function() {
    var res = JSON.parse(this.response)
    if (res.status == 'success') {
      $(event.srcElement).addClass('saved')
    }
  })
  xhr.send(JSON.stringify({
    url,
    tag: event.srcElement.dataset.tag
  }))
}

function MakeDraggable(element) {
  $(element).draggable({
    start: function(event, ui) {
      $(this).addClass('dragging')
      $('#pixabay-results, #image-browse-results').css('overflow', 'visible')
      control.image_id = ParseImageId(element)
    },
    stop: function(event, ui) {
      $(this).removeClass('dragging')
      $('#pixabay-results, #image-browse-results').css('overflow', 'auto')
      $('.tile').removeClass('over')
    },
    revert: true
  })
}

var drop_blocks = []
function MakeDroppable(element) {
   $(element).droppable({
    drop: function(event, ui) {
      console.log(drop_blocks)
      SaveBlocks(drop_blocks)
    },
    over: function(event, ui) {
      $('.tile').removeClass('over')
      drop_blocks = []
      const dimensions = Math.abs(Math.floor($(".drop-dimensions input").filter(':visible').val()))
      const xRepeat = Math.abs(Math.floor($(".x-repeat").filter(':visible').val()))
      const yRepeat = Math.abs(Math.floor($(".y-repeat").filter(':visible').val()))
      var id = event.target.id.replace('tile_', '').split('-').map(Number)
      for (var y = id[0]; y < id[0] + (dimensions * yRepeat); y += dimensions) {
        for (var x = id[1]; x < id[1] + (dimensions * xRepeat); x += dimensions) {
          drop_blocks.push({
            dimension: [dimensions],
            start: [y, x],
            end: [y + dimensions - 1, x + dimensions - 1]
          })
          for (var i = y; i < y + dimensions; i++) {
            for (var j = x; j < x + dimensions; j++) {
              if (i < view.dimension && j < view.dimension) {
                $(`#tile_${i}-${j}`).addClass('over')
              }
            }
          }
        }
      }
    }
  })
}

function SaveBlocks(drop_blocks, image_id) {
  var xhr = new XMLHttpRequest()
  const url = '/save-blocks/' + player.level + '/' + control.image_id
  xhr.open('POST', url)
  xhr.addEventListener('load', function() {
    var res = JSON.parse(this.response)
    if (res.status == 'success') {
      LoadView()
    }
    control.image_id = ''
  })
  xhr.send(JSON.stringify(drop_blocks))
}

/**
 * 
  "id": "90dd3c12-7dff-40e4-be7b-fc39926daeb3",
  "user_name": "peter",
  "start_y": 3,
  "start_x": 14,
  "end_y": 3,
  "end_x": 14,
  "level_id": 1,
  "image_id": "d5cb7c95-3db7-40a5-a3e2-55d77dfadf42",
  "background_size": "contain",
  "background_repeat": "no-repeat",
  "translate_x": 0,
  "translate_y": 0,
  "scale": 1,
  "dimension": 1
  *
  */
function CreateAndAddBlock(block) {
  var div = document.createElement("div");
  div.classList.add('block');
  div.style.background = `url(/get-image/${block.image_id})`;
  div.style.backgroundSize = block.background_size;
  div.style.backgroundRepeat = block.background_repeat;
  div.style.transform = `translateX(${block.translate_x}px) translateY(${block.translate_y}px) scale(${block.scale})`;
  var startTile = document.getElementById(`tile_${block.start_y}-${block.start_x}`);
  var endTile = document.getElementById(`tile_${block.end_y}-${block.end_x}`);
  var tileWidth = +getComputedStyle(startTile).width.split('px')[0] * block.dimension
  var tileHeight = tileWidth
  div.style.width = tileWidth + 'px'
  div.style.height = tileHeight + 'px'
  div.style.top = startTile.offsetTop + 'px'
  div.style.left = startTile.offsetLeft + 'px'
  document.getElementById('view').appendChild(div)
}

function GetBlocks() {
  document.querySelectorAll('.block', function(elem) {
    element.remove()
  })
  var xhr = new XMLHttpRequest();
  xhr.open("GET", '/get-blocks/' + player.level);
  xhr.addEventListener("load", function() {
    var res = JSON.parse(this.response)
    if (res.status == 'success') {
      for (var block of res.data) {
        CreateAndAddBlock(block)
      }
    }
  })
  xhr.send()
}

function LoadView() {
  GetBlocks()
}

function LoadImageIds() {
  var xhr = new XMLHttpRequest()
  xhr.open('GET', '/get-image-ids')
  xhr.addEventListener('load', function() {
    var res = JSON.parse(this.response)
    if (res.status == 'success') {
      var imageBrowseSelect = document.getElementById('image-browse-select')
      imageBrowseSelect.innerHTML = '<option>select tag</option>'
      var imageBrowseResults = document.getElementById('image-browse-results')
      imageBrowseResults.innerHTML = ''
      control.image_urls = {}
      var tags = Array.from(new Set(res.data.map(d => d.tag)))
      for (var i = 0; i < tags.length; i++) {
        var option = document.createElement("option")
        option.value = tags[i]
        option.innerHTML = tags[i]
        imageBrowseSelect.appendChild(option)
        if (!control.image_urls[tags[i]]) {
          control.image_urls[tags[i]] = []
        }
      }
      for (var i = 0; i < res.data.length; i++) {
        control.image_urls[res.data[i].tag].push(`/get-image/${res.data[i].id}`)
      }
    }
  })
  xhr.send()
}

function SelectTag(event) {
  var tag = event.srcElement.value
  var results = document.getElementById('image-browse-results')
  results.innerHTML = ''
  for (var image_url of control.image_urls[tag]) {
    console.log(image_url)
    AppendImageDiv(results, image_url, tag, '.image')
  }
}

$( function() {
  console.log('starting...')
  player = {
    name: getCookieValue("name"),
    level: getCookieValue("level"),
    position_x: getCookieValue("position_x"),
    position_y: getCookieValue("position_y")
  }

  GetBlocks()

  document.querySelector("#log p").innerHTML = Object.keys(player).map(key => `<div><strong>${key}:</strong> <span>${player[key]}</span></div>`).join('')

  var view = document.getElementById('view')
  for (var i = 0; i < 400; i++) {
    var tile = document.createElement('div')
    tile.id = `tile_${ Math.floor(i / 20) }-${ i % 20 }`
    tile.classList.add('tile')
    MakeDroppable(tile)
    view.appendChild(tile)
  }

  for (var key in events) {
    var split_key = key.split(':')
    document.querySelectorAll(split_key[0]).forEach(element => {
      console.log(key[1], element)
      element.addEventListener(split_key[1], events[key])
    })
  }

  $('#tabs').tabs({
    activate: function(event, ui) {
      console.log('activate')
      switch (event.delegateTarget.href.split('/').pop()) {
      case "#browse-images":
        LoadImageIds()
        break;
      default: 
        break;
      }
    }
  })
} )