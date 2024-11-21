var events = {
  '#log-out:click': Logout,
  '#search-pixabay:click': SearchPixabay,
  '#pixabay-results:scroll': ScrollPixabayResults,
  '.pixabay-result:dblclick': SaveImage,
  '#image-browse-select:change': SelectTag,
  '.block:click': ClickBlock,
  '.object-area-preview:mousedown': ObjectAreaPreviewMousedown,
  '.object-area-preview:mousemove': ObjectAreaPreviewMousemove,
  '.object-area-preview:mouseup': ObjectAreaPreviewMouseup,
  '#block-css:keyup': ValidateBlockCSS,
  '#apply-block-css:click': ApplyBlockCSS
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
  dimension: 20,
  blocks: {}
}

var control = {
  image_urls: {},
  selection_box: []
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

function AddEvents(selector) {
  for (var key in events) {
    var split_key = key.split(':')
    if (selector) {
      if (split_key[0] == selector) {
        document.querySelectorAll(split_key[0]).forEach(element => {
          element.removeEventListener(split_key[1], events[key])
          element.addEventListener(split_key[1], events[key])
        })
      }
    } else {
      document.querySelectorAll(split_key[0]).forEach(element => {
        element.removeEventListener(split_key[1], events[key])
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
  var id = element.style.background.replace('url(', '').replace(')', '').replaceAll("'", '').replaceAll('"', '');
  if (!/pixabay/.test(id)) {
    return id.split('/').pop()
  } else {
    return id
  }
}

function SaveImage(event, callback) {
  var url = event ? event.srcElement.style.background.replace('url(', '').replace(')', '').replaceAll("'", '').replaceAll('"', '') : control.pixabay_url
  var xhr = new XMLHttpRequest()
  xhr.open('POST', '/save-image')
  xhr.addEventListener('load', function() {
    var res = JSON.parse(this.response)
    if (res.status == 'success') {
      control.image_id = res.id
      if (event) {
        $(event.srcElement).addClass('saved')
      }
      if (callback) {
        callback()
      }
    }
  })
  xhr.send(JSON.stringify({
    url,
    tag: event ? event.srcElement.dataset.tag : $("#pixabay-search").val()
  }))
}

function MakeDraggable(element) {
  $(element).draggable({
    start: function(event, ui) {
      $(this).addClass('dragging')
      $('#pixabay-results, #image-browse-results').css('overflow', 'visible')
      let image_id = ParseImageId(element)
      if (/pixabay/.test(image_id)) {
        control.pixabay_url = image_id
      } else {
        control.image_id = image_id
      }
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
      if (control.image_id && !control.image_id == "") {
        SaveBlocks(drop_blocks)
      } else {
        SaveImage(null, function() {
          SaveBlocks(drop_blocks)
        })
      }
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

function SaveBlocks(drop_blocks) {
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
  

  var css = JSON.parse(block.css)
  css.backgroundImage = `url(/get-image/${block.image_id})`
  $(div).css(css)

  var startTile = document.getElementById(`tile_${block.start_y}-${block.start_x}`);
  var endTile = document.getElementById(`tile_${block.end_y}-${block.end_x}`);
  var tileWidth = +getComputedStyle(startTile).width.split('px')[0] * block.dimension
  var tileHeight = tileWidth
  div.style.width = tileWidth + 'px'
  div.style.height = tileHeight + 'px'
  div.style.top = startTile.offsetTop + 'px'
  div.style.left = startTile.offsetLeft + 'px'
  div.id = block.id
  document.getElementById('view').appendChild(div)
  view.blocks[block.id] = {
    block,
    div
  }
}



function GetBlocks() {
  view.blocks = {}
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
      AddEvents()
    }
  })
  xhr.send()
}

function LoadView() {
  $('.block').each(function(_, elem) {
    elem.remove()
  })
  GetBlocks()
  $('#block-image-edit-area').css('height', $('#block-image-edit-area').css('width'))
  $('.object-area').css('height', $(".object-area").css('width'))
  $('.object-area-preview').css('height', $(".object-area-preview").css('width'))
  $('#block-image').css('height', $("#block-image-edit-area").css('width'))
  $('#block-image').css('width', $("#block-image-edit-area").css('width'))
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

function ClickBlock(event) {
  let block = view.blocks[event.srcElement.id]
  $('.block').removeClass('editing')
  block.div.classList.add('editing')
  $('#ui-id-3').click()
  var css = JSON.parse(block.block.css)
  css.backgroundImage = `url(/get-image/${block.block.image_id})`
  $('#block-image').css(css)
  $('#block-css').html(JSON.stringify(JSON.parse(block.block.css), null, 1))
  ValidateBlockCSS()
  $('.object-area-preview').css('height', $(".object-area-preview").css('width'))
}

function ObjectAreaPreviewMousedown(event) {}
function ObjectAreaPreviewMousemove(event) {}
function ObjectAreaPreviewMouseup(event) {}

function isValidJson(jsonString) {
    try {
        JSON.parse(jsonString);
        return true;
    } catch (e) {
        return false;
    }
}


function isValidCSS(rules) {
    const tempElement = document.createElement('div');
    for (let property in rules) {
        if (rules.hasOwnProperty(property)) {
            tempElement.style[property] = rules[property];
            if (tempElement.style[property] === '') {
                return false;
            }
        }
    }
    return true;
}


function ValidateBlockCSS() {
  var css = $("#block-css").val()
  console.log('css', css)
  if (!isValidJson(css)) {
    $("#block-css").addClass('invalid')
  } else if (!isValidCSS(JSON.parse(css))) {
    $("#block-css").addClass('invalid')
  } else {
    $("#block-css").removeClass('invalid')
  }
}

function ApplyBlockCSS() {
  if (!$("#block-css").hasClass('invalid')) {
    var newCSS = JSON.parse($("#block-css").val());
    var biea = document.querySelector('#block-image')
    for (var key in newCSS) {
      biea.style[key] = newCSS[key]
    }
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

  document.querySelector("#log p").innerHTML = Object.keys(player).map(key => `<div><strong>${key}:</strong> <span>${player[key]}</span></div>`).join('')

  var view = document.getElementById('view')
  for (var i = 0; i < 400; i++) {
    var tile = document.createElement('div')
    tile.id = `tile_${ Math.floor(i / 20) }-${ i % 20 }`
    tile.classList.add('tile')
    MakeDroppable(tile)
    view.appendChild(tile)
  }

  var biea = document.getElementById('block-image-edit-area')
  for (var i = 0; i < 49; i++) {
    var object_area_preview = document.createElement('div')
    object_area_preview.classList.add('object-area-preview')
    biea.appendChild(object_area_preview)
  }

  LoadView()

  for (var key in events) {
    var split_key = key.split(':')
    document.querySelectorAll(split_key[0]).forEach(element => {
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


  window.addEventListener('resize', function() {
    LoadView();
  })

} )







