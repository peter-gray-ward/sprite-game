var events = {
  '#log-out:click': Logout,
  '#search-pixabay:click': SearchPixabay,
  '#image-browse-results:scroll': ScrollPixabayResults,
  '.pixabay-result:dblclick': SaveImage,
  '#image-browse-select:change': SelectTag,
  '.block:click': SelectBlock,
  '.object-area-preview:mousedown': ObjectAreaPreviewMousedown,
  '.object-area-preview:mousemove': ObjectAreaPreviewMousemove,
  '.object-area-preview:mouseup': ObjectAreaPreviewMouseup,
  '#block-css:keyup': ValidateBlockCSS,
  '#apply-block-edits:click': ApplyBlockEdits,
  '#view-object-areas:change': ChangeViewObjectAreas,
  '#delete-block:click': DeleteBlock,
  '.tile:click': DeselectBlock,
  '#manage-blocks .tbody .tr:click': SelectManageBlockRow,
  '#select-parent-id:click': SelectParentId,

  'window:keydown': KeyDown,
  'window:keyup': KeyUp
}

Array.prototype.contains = function(str) {
  for (var i = 0; i < this.length; i++) {
    if (this[i] == str) {
      return true
    }
  }
  return false
}

function include(obj, inclusions) {
  var result = {}
  for (var key in obj) {
    if (inclusions.contains(key)) {
      result[key] = obj[key]
    }
  }
  return result
}

function omit(obj, omissions) {
  var result = {}
  for (var key in obj) {
    if (!omissions.contains(key)) {
      result[key] = obj[key]
    }
  }
  return result
}

var view = {
  dimension: 20,
  blocks: {},
  $height: undefined,
  drop_area: {},
  view_block_areas: false,
  saving_object_areas: true,
  object_areas: [],
  wall_bump_sound: new Audio("/wall-bump-sound.mp3"),
  sprite: {
    name: 'Gandalf',
    el: undefined,
    position: {
      left: 0,
      top: 0
    },
    keys: new Set(),
    key_values: {
      arrowup: {
        top: -.002,
      },
      arrowright: {
        left: .002
      },
      arrowdown: {
        top: .002
      },
      arrowleft: {
        left: -.002
      }
    },
    rotation: {
      up: {
        top: -517 / -1344,
        index: 0
      },
      left: {
        top: -582 / -1344,
        index: 0
      },
      down: {
        top: -646 / -1344,
        index: 0
      },
      right: {
        top: -710 / -1344,
        index: 0
      }
    },
    section: -64 / -832,
    offset: -2 / -832,
    direction: undefined,
    z_index: 0
  }
}

var control = {
  image_urls: {},
  object_area_preview: {
    active: false,
    object_area: []
  },
  needsUpdate: true,
  selecting_parent_id: false
}

var player = {}

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
    if (split_key[0] == 'window') {
      window.addEventListener(split_key[1], events[key]);
    } else {
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
}

function Pixabay(term, page) {
  return new Promise(resolve => {
    term = term.trim().split(' ').join('+')
    var image_type = $('#search-vector').is(':checked') ? 'vector' : 'all'
    var xhr = new XMLHttpRequest()
    xhr.open('GET', `https://pixabay.com/api/?key=25483695-93658ed46b8876fc2d6419379&q=${term}&per_page=25&image_type=${image_type}&page=${page}`)
    xhr.addEventListener('load', function() {
      try {
        resolve(JSON.parse(this.response))
      } catch (error) {
        console.error(error)
      }
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
    var results = document.getElementById('image-browse-results')
    results.classList.add('pixabay-results')
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
  if ($('#image-browse-results').hasClass('pixabay-results')) {
    const scrollableElement = event.target;
    console.log(scrollableElement.scrollHeight - scrollableElement.scrollTop, scrollableElement.clientHeight)
    const isAtBottom = scrollableElement.scrollHeight - scrollableElement.scrollTop <= scrollableElement.clientHeight + .5;
    if (isAtBottom) {
      SearchPixabay(null, false);
    }
  }
}

function ParseBackgroundImageId(element) {
  var id = element.style.background.replace('url(', '').replace(')', '').replaceAll("'", '').replaceAll('"', '');
  if (id == "") {
    id = element.style.backgroundImage.replace('url(', '').replace(')', '').replaceAll("'", '').replaceAll('"', '');
  }
  if (!/pixabay/.test(id)) {
    return id.split('/').pop()
  } else {
    return id
  }
}

function SaveImage(event, callback) {
  var tag = event ? event.srcElement.dataset.tag : $("#pixabay-search").val()
  var url = event ? event.srcElement.style.background.replace('url(', '').replace(')', '').replaceAll("'", '').replaceAll('"', '') : control.pixabay_url
  var xhr = new XMLHttpRequest()
  xhr.open('POST', '/save-image')
  xhr.addEventListener('load', function() {
    let res
    try {
      res = JSON.parse(this.response)
    } catch (err) {
      window.location.reload()
    }
    if (res.status == 'success') {
      control.image_id = res.id
      if (event) {
        $(event.srcElement).addClass('saved')
      }
      if (!control.image_urls[tag]) {
        LoadImageIds()
      }
      if (callback) {
        callback()
      }
    }
  })
  xhr.send(JSON.stringify({
    url,
    tag
  }))
}

function MakeDraggable(element) {
  $(element).draggable({
    start: function(event, ui) {
      $('.tile').css('z-index', 999);
      $(this).addClass('dragging');
      $('#image-browse-results, #image-browse-results').css('overflow', 'visible')

      if (Boolean(this.dataset.copy) == true) {
        control.copy = true
      }
      
      if (event.currentTarget.classList.contains('block')) {
        control.dragged_tile_id = event.currentTarget.id
      } else {
        let image_id = ParseBackgroundImageId(element)
        if (/pixabay/.test(image_id)) {
          control.pixabay_url = image_id
        } else {
          control.image_id = image_id
        }
      }
    },
    stop: function(event, ui) {
      $('.tile').css('z-index', 'initial')
      $(this).removeClass('dragging')
      $('#image-browse-results, #image-browse-results').css('overflow', 'auto')
      $('.tile').removeClass('over')
    },
    revert: true
  })
}

function MakeDroppable(element) {
   $(element).droppable({
    drop: function(event, ui) {
      if (control.dragged_tile_id) {
        ChangeBlockPosition()
        control.dragged_tile_id = null
      } else if (control.image_id && !control.image_id == "") {
        SaveBlocks()
      } else {
        SaveImage(null, function() {
          SaveBlocks()
        })
      }

      control.copy = false
    },
    over: function(event, ui) {
      $('.tile').removeClass('over')

      const dimensions = Math.abs(Math.floor($(".drop-dimensions input").val()))
      const xRepeat = Math.abs(Math.floor($(".x-repeat").val()))
      const yRepeat = Math.abs(Math.floor($(".y-repeat").val()))
      const xDir = Math.abs(Math.floor($(".x-dir").val()))
      const yDir = Math.abs(Math.floor($(".y-dir").val()))

      var id = event.target.id.replace('tile_', '').split('-').map(Number)

      $('#tile-over-id').html(id[1] + ',' + id[0])

      view.drop_area = {
        dimensions: control.copy ? view.blocks[control.block_id].block.dimension : dimensions,
        xRepeat: xRepeat, 
        yRepeat: yRepeat,
        xDir: xDir, 
        yDir: yDir,
        start_x: id[1],
        start_y: id[0]
      }

      for (var y = id[0]; y < id[0] + (dimensions * yRepeat); y += dimensions * (yDir / Math.abs(yDir))) {
        for (var x = id[1]; x < id[1] + (dimensions * xRepeat); x += dimensions * (xDir / Math.abs(xDir))) {
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

function SaveBlocks() {
  var xhr = new XMLHttpRequest()
  const url = '/save-blocks/' + player.level + '/' + control.image_id
  xhr.open('POST', url)
  xhr.addEventListener('load', function() {
    let res
    try {
      res = JSON.parse(this.response)
    } catch (err) {
      window.location.reload()
    }
    if (res.status == 'success') {
      control.needsUpdate = true
      LoadView()
    }
    control.image_id = ''
  })

  xhr.send(JSON.stringify(view.drop_area))
}

function GetBlocks() {
  view.blocks = {}
  document.querySelectorAll('.block', function(elem) {
    element.remove()
  })
  var xhr = new XMLHttpRequest();
  xhr.open("GET", '/get-blocks/' + player.level);
  xhr.addEventListener("load", function() {
    let res
    try {
      res = JSON.parse(this.response)
    } catch (err) {
      window.location.reload()
    }
    if (res.status == 'success') {
      control.needsUpdate = false
      var blocks = res.data.map(block => {
        block.object_area = JSON.parse(block.object_area)
        block.css = JSON.parse(block.css)
        return block
      });
      RenderManageBlocks(blocks)
      RenderBlocks(blocks)
    }
  })
  xhr.send()
}

function RenderManageBlocks(blocks) {
  var keys = ['id', 'recurrence_id', 'image_id']
  document.getElementById("manage-blocks").innerHTML = `
    <div class="table">
      <div class="thead">
        <div class="tr">
          ${
            keys.map(key => `<div class="th">${key}</div>`).join('')
          }
        </div>
      </div>
      <div class="tbody">
        ${
          blocks.map(block => `
            <div class="tr">
              ${
                keys.map(key => {
                  switch (key) {
                  case 'image_id': return `<div class="td image" style="background-image: url(/get-image/${block[key]})"></div>`
                  default: return `<div class="td">${block[key]}</div>`
                  }
                }).join('')
              }
            </div>
          `).join('')
        }
      </div>
    </div>
  `;
}

function RenderBlocks(blocks) {
  for (var block of blocks) {
    
    for (var y = block.start_y; 
      (block.dir_y > 0 ? y < block.start_y + (block.dimension * block.repeat_y) : y > block.start_y + (block.dimension * block.repeat_y)); 
      y += block.dimension * (block.dir_y / Math.abs(block.dir_y))) {
      
      for (var x = block.start_x; 
        (block.dir_x > 0 ? x < block.start_x + (block.dimension * block.repeat_x) : x > block.start_x + (block.dimension * block.repeat_x)); 
        x += block.dimension * (block.dir_x / Math.abs(block.dir_x))) {
        
        let renderedBlock = Object.assign({}, block);
        renderedBlock.start_x = x;
        renderedBlock.start_y = y;

        var div = document.createElement("div");
        div.id = renderedBlock.id + '::' + y + '_' + x
        div.classList.add('block');
        if (renderedBlock.id == control.block_id) {
          div.classList.add('editing')
        }

        var css = Object.assign({}, renderedBlock.css);
        css.backgroundImage = `url(/get-image/${renderedBlock.image_id})`

        if (block.random_rotation > 0) {
          var randomRotation = 'rotate(' + Math.floor(Math.random() * 4) * 90 + 'deg)';
          
          if (/rotate\(.+\)/.test(block.css.transform)) {
            block.css.transform = block.css.transform.replace(/rotate\(.+\)/, randomRotation);
          } else {
            block.css.transform += ' ' + randomRotation;
          }
        }

        if (css.zIndex) {
          delete css.zIndex
        }

        $(div).css(css)

        let startY = renderedBlock.start_y < 0 ? 0 : renderedBlock.start_y
        let startX = renderedBlock.start_x < 0 ? 0 : renderedBlock.start_x
        startY = startY > 19 ? 19 : startY
        startX = startX > 19 ? 19 : startX
        var startTile = document.getElementById(`tile_${startY}-${startX}`);
        var tileWidth = +getComputedStyle(startTile).width.split('px')[0] * renderedBlock.dimension
        var tileHeight = tileWidth
        div.style.width = tileWidth + 'px'
        div.style.height = tileHeight + 'px'
        div.style.top = startTile.offsetTop + 'px'
        div.style.left = startTile.offsetLeft + 'px'
        div.dataset.id = block.id
        div.dataset.recurrence_id = renderedBlock.recurrence_id;

        document.getElementById('view').appendChild(div)

        MakeDraggable(div)

        if (!view.blocks[renderedBlock.id]) {
          view.blocks[renderedBlock.id]  = {
            block: renderedBlock,
            divs: [],
            children_ids: []
          }
        }

        view.blocks[renderedBlock.id].divs.push(div)
      }
    }
  }

  AddObjectAreas()
  SetZIndexes()
  AdjustEditBlockImage()
  AddEvents()
}

function SetZIndexes() {
  for (var id in view.blocks) {
    if (view.blocks[id].block.ground) {
      for (var div of view.blocks[id].divs) {
        $(div).css('z-index', 1)
      }
    } else {
      for (var div of view.blocks[id].divs) {
        if (!view.blocks[id].block.parent_id || view.blocks[id].block.parent_id.length !== 36) {
          var bottom = view.blocks[id].divs.length == 1 ? Infinity : Math.floor(+$(div).css('top').split('px')[0] + $(div).height())
          if (bottom == Infinity) {
            for (var oa of view.object_areas) {
              if (oa.block_id == id) {
                var oaBottom = oa.top + oa.height
                if (oaBottom < bottom) {
                  bottom = Math.floor(oaBottom);
                }
              }
            }
            var divBottom = Math.floor(+$(div).css('top').split('px')[0] + $(div).height());
            if (divBottom < bottom) {
              bottom = divBottom
            } 
          }
          $(div).css("z-index", bottom);
          for (var id2 in view.blocks) {
            if (view.blocks[id2].block.parent_id == view.blocks[id].block.id) {
              for (var div2 of view.blocks[id2].divs) {
                $(div2).css('z-index', bottom + 1)
              }
            }
          }
        }
      }
    }
  }
}

function LoadView() {
  view.saving_object_areas = true
  $('.block').each(function(_, elem) {
    elem.remove()
  })
  $('#gandalf').remove()
  if (control.needsUpdate) {
    GetBlocks()
  } else {
    RenderBlocks(Object.values(view.blocks).map(block => block.block))
  }
  $('.object-area-preview').css('height', $(".object-area-preview").css('width'))
  $('#block-image-container').css('height', $("#block-image-edit-area").css('width'))

  LoadSprite()
  view.$height = $("#view").height()
}

function LoadSprite() {
  view.sprite.direction = player.direction
  view.sprite.el = document.createElement("div")
  view.sprite.el.id = "gandalf"
  view.sprite.el.classList.add('sprite')
  document.querySelector('main').appendChild(view.sprite.el)
  RenderGandalf()
}

var i = 0;
function RenderGandalf() {
  let isMoving = false;
  const originalPosition = Object.assign({}, view.sprite.position)
  for (var key of view.sprite.keys) {
    if (view.sprite.key_values[key]) {
      isMoving = true;
      for (var pos in view.sprite.key_values[key]) {
        view.sprite.position[pos] += view.sprite.key_values[key][pos];
      }
    }
  }

  if (isMoving && view.sprite.direction) {
    view.sprite.rotation[view.sprite.direction].index++;
    if (view.sprite.rotation[view.sprite.direction].index > 8) {
      view.sprite.rotation[view.sprite.direction].index = 0;
    }
  }

  const spriteDirection = view.sprite.direction;
  const spriteSize = 64; // Size of each sprite in pixels
  const imageHeight = 1344; // Total height of the sprite sheet in pixels
  const scaledImageHeight = 189; // Total height of the sprite sheet in vh
  const scalingFactor = 189 / 1344;
  const rotation = view.sprite.rotation[spriteDirection];
  const sectionWidth = view.sprite.section * 832;
  const offset = view.sprite.offset * 832;
  const backgroundX = `calc(-1 * (${rotation.index} * 64) * ${scalingFactor}vh)`;
  const backgroundY = `calc(-1 * ${rotation.top * 1344} * ${scalingFactor}vh)`;

  var sprite$viewHeight = spriteSize * scalingFactor;
  var css = {
    background: `url(/Gandalf.png) no-repeat`,
    backgroundSize: `auto ${scaledImageHeight}vh`,
    backgroundPosition: i % 8 === 0 ? `${backgroundX} ${backgroundY}` : $("#gandalf").css("background-position"),
    width: `${sprite$viewHeight}vh`, // Ensure sprite section matches 9vh
    height: `${sprite$viewHeight}vh`,
    top: view.sprite.position.top * view.$height,
    left: view.sprite.position.left * view.$height,
    zIndex: Math.floor(view.sprite.position.top * view.$height + sprite$viewHeight * 0.01 * 0.75 * view.$height)
  }

  if (i++ == 100) {
    i = 0
  }

  let spriteTop = view.sprite.position.top * view.$height;
  let spriteBottom = view.sprite.position.top * view.$height + $("#gandalf").height();
  let spriteRight = view.sprite.position.left * view.$height + $("#gandalf").width();
  let spriteLeft = view.sprite.position.left * view.$height;
  const gandalf$height = $("#gandalf").height();
  const gandalf$width = $("#gandalf").width()


  spriteTop += gandalf$height * .75
  spriteBottom -= gandalf$height * .15
  spriteLeft += gandalf$width * 0.333
  spriteRight -= gandalf$width * 0.333

  var bumps = false
  for (var object_area of view.object_areas) {
    switch (view.sprite.direction) {
    case "up":
      if (spriteTop <= object_area.top + object_area.height + 1
        && spriteBottom > object_area.top + object_area.height
        && spriteRight >= object_area.left 
        && spriteLeft <= object_area.left + object_area.width) {
        bumps = true
        $('#' + object_area.id).css('background', 'rgba(0,0,255,0.333)')
      } else {
        $('#' + object_area.id).css('background', 'rgba(255,0,0,0.333)')
      }
      break;
    case "right":
      if (spriteRight >= object_area.left - 1
        && spriteLeft < object_area.left
        && spriteTop <= object_area.top + object_area.height
        && spriteBottom >= object_area.top) {
         bumps = true
        $('#' + object_area.id).css('background', 'rgba(0,0,255,0.333)')
      } else {
        $('#' + object_area.id).css('background', 'rgba(255,0,0,0.333)')
      }
      break;
    case "down":
      if (spriteBottom >= object_area.top - 1
        && spriteTop < object_area.top
        && spriteRight >= object_area.left 
        && spriteLeft <= object_area.left + object_area.width) {
        bumps = true
        $('#' + object_area.id).css('background', 'rgba(0,0,255,0.5)')
      } else {
        $('#' + object_area.id).css('background', 'rgba(255,0,0,0.5)')
      }
      break;
    case "left":
      if (spriteLeft <= object_area.left + object_area.width + 1
        && spriteRight > object_area.left + object_area.width
        && spriteTop <= object_area.top + object_area.height
        && spriteBottom >= object_area.top) {
        bumps = true
        $('#' + object_area.id).css('background', 'rgba(0,0,255,0.333)')
      } else {
        $('#' + object_area.id).css('background', 'rgba(255,0,0,0.333)')
      }
      break;
    }
  }

  if (bumps) {
    if (isMoving) {
      view.wall_bump_sound.play()
    }
    delete css.top
    delete css.left
    view.sprite.position = originalPosition
  } else {
    css.top += 'px'
    css.left += 'px'
  }

  

  $(view.sprite.el).css(css);

  window.requestAnimationFrame(RenderGandalf);
}

function LoadImageIds() {
  var xhr = new XMLHttpRequest()
  xhr.open('GET', '/get-image-ids')
  xhr.addEventListener('load', function() {
    let res
    try {
      res = JSON.parse(this.response)
    } catch (err) {
      window.location.reload()
    }
    if (res.status == 'success') {
      var imageBrowseSelect = document.getElementById('image-browse-select')
      imageBrowseSelect.innerHTML = '<option>select tag</option>'
      var imageBrowseResults = document.getElementById('image-browse-results')
      imageBrowseResults.innerHTML = ''
      control.image_urls = {}
      var tags = Array.from(new Set(res.data.map(d => d.tag))).sort((a, b) => a > b ? 1 : -1)
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
  results.classList.remove('pixabay-results')
  results.innerHTML = ''
  for (var image_url of control.image_urls[tag]) {
    console.log(image_url)
    AppendImageDiv(results, image_url, tag, '.image')
  }
}

function AdjustEditBlockImage() {
  var biea = document.getElementById('block-image-edit-area')
  var width = +getComputedStyle(biea).width.split('px')[0]
  var height = +getComputedStyle(biea).height.split('px')[0]

  $('#block-image-container, #block-image').css({
    width: width + 'px',
    height: height + 'px'
  })
}

function SelectBlock(event) {

  if (control.selecting_parent_id) {
    $("#parent-id").html(event.srcElement.dataset.id)
    control.selecting_parent_id = false;
    $("#select-parent-id").css({
      background: 'slategray',
      color: 'lawngreen'
    })
    return;
  }

  $('.block').removeClass('editing')

  control.block_id = event.srcElement.dataset.id
  view.blocks[control.block_id].divs.forEach(div => {
    $(div).addClass('editing')
  });

  const block = view.blocks[control.block_id].block;
  $("#block-type-edit .y-start").val(block.start_y)
  $("#block-type-edit .x-start").val(block.start_x)
  $("#block-type-edit .dimension").val(block.dimension)
  $("#block-type-edit .y-repeat").val(block.repeat_y)
  $("#block-type-edit .x-repeat").val(block.repeat_x)
  $("#block-type-edit .y-dir").val(block.dir_y)
  $("#block-type-edit .x-dir").val(block.dir_x)
  $("#block-type-edit #translate-object-area").prop('checked', block.translate_object_area > 0)
  $("#block-type-edit #random-rotation").prop('checked', block.random_rotation > 0)
  $("#block-type-edit #ground").prop('checked', block.ground > 0)
  $("#parent-id").html(block.parent_id)

  $('a[href="#edit-block"]').click()
  $('#ui-id-3').click()

  var css = Object.assign({}, view.blocks[control.block_id].block.css);

  $('#block-css').val(JSON.stringify(css, null, 1))

  css.backgroundImage = `url(/get-image/${view.blocks[control.block_id].block.image_id})`;
  css.transform = css.transform.replace(/scale\(\d+(\.\d+)?\)/, '')

  $('#block-image').css(css)
  $('#block-type-edit .drop-dimensions input').val(view.blocks[control.block_id].block.dimension)

  let copyBlock = document.getElementById('copy-block')
  $(copyBlock).css('background-image', css.backgroundImage);
  copyBlock.dataset.copy = true
  copyBlock.dataset.id = block.id
  MakeDraggable(copyBlock)

  ValidateBlockCSS()
  AdjustEditBlockImage()

  $('.object-area-preview').css('height', $(".object-area-preview").css('width'))
  $('.object-area-preview.selected').removeClass('selected')

  for (var object_area of view.blocks[control.block_id].block.object_area) {
    $(`#object-area-${object_area[0]}_${object_area[1]}`).addClass('selected')
  }
}

function DeselectBlock(event) {
  $('.block').removeClass('editing')
  control.block_id = null
  $('#block-type-edit input').val(0)
  $('#block-image').css({
    'background-image': 'initial',
    'transform': 'initial'
  });
  $('.object-area-preview').removeClass('selected')
  $('#block-css').html('')
}

function SelectManageBlockRow(event) {
  var tr = event.srcElement;
  while (tr && !tr.classList.contains('tr')) {
    tr = tr.parentElement;
  }
  SelectBlock({
    srcElement: {
      dataset: {
        id: tr.children[0].innerHTML
      }
    }
  })
}

function ObjectAreaPreviewMousedown(event) {
  event.preventDefault()
  control.object_area_preview.active = true
  $(event.target).toggleClass('selected')
}

function ObjectAreaPreviewMousemove(event) {
  if (control.object_area_preview.active) {
    $(event.target).addClass('selected')
  }
}

function ObjectAreaPreviewMouseup(event) {
  control.object_area_preview.object_area = []
  setTimeout(function() {
    $(event.target.parentElement).find('.selected').each(function(_, elem) {
      var id = $(elem)[0].id
      id = id.replace('object-area-', '').split('_').map(Number)
      control.object_area_preview.object_area.push(id)
    })
    UpdateBlock()
    control.object_area_preview.active = false
    control.needsUpdate = true
  }, 0)
}

function UpdateBlock() {
  var xhr = new XMLHttpRequest()
  xhr.open('POST', `/update-block-object-area/${view.blocks[control.block_id].block.recurrence_id}`)
  xhr.addEventListener('load', function() {
    var res = JSON.parse(this.response)
    if (res.status == 'success') {
      var newObjectArea = JSON.parse(res.data)
      UpdateBlockCache(view.blocks[control.block_id].block.recurrence_id, 'object_area', newObjectArea)
      LoadView()
    }
  })
  xhr.send(JSON.stringify({
    object_area: JSON.stringify(control.object_area_preview.object_area)
  }))
}

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

function UpdateBlockCache(recurrence_id, key, value) {
  if (typeof key == 'object') {
    for (var id in view.blocks) {
      if (view.blocks[id].block.recurrence_id == recurrence_id) {
        for (var _key in key) {
          view.blocks[id].block[_key] = key[_key]
        }
      }
    }
  } else {
    for (var id in view.blocks) {
      if (view.blocks[id].block.recurrence_id == recurrence_id) {
        view.blocks[id].block[key] = value
      }
    }
  }
}

function DeleteFromBlockCache(recurrence_id) {
  for (var id in view.blocks) {
    if (view.blocks[id].block.recurrence_id == recurrence_id) {
      console.log("deleting block " + id + " from view.blocks...");
      delete view.blocks[id];
    }
  }
}

function ValidateBlockCSS() {
  var css = $("#block-css").val()
  if (!isValidJson(css)) {
    $("#block-css").addClass('invalid')
  } else if (!isValidCSS(JSON.parse(css))) {
    $("#block-css").addClass('invalid')
  } else {
    $("#block-css").removeClass('invalid')
  }
}

function ApplyBlockEdits() {

  // CSS
  if (!$("#block-css").hasClass('invalid')) {
    var newCSS = JSON.parse($("#block-css").val());
    var biea = document.querySelector('#block-image')
    for (var key in newCSS) {
      if (key == 'transform') {
        var css = newCSS[key]
        css = css.replace(/scale(.+)/, '')
        biea.style[key] = css
      } else {
        biea.style[key] = newCSS[key]
      }
    }

    var css = Object.assign({}, newCSS);
    css.transform = css.transform.replace(/scale\(\d+(\.\d+)?\)/, '')

    $('#block-image').css(css)
    
    UpdateBlockCache(view.blocks[control.block_id].block.recurrence_id, 'css', newCSS)
    $(`.block[data-recurrence_id="${view.blocks[control.block_id].block.recurrence_id}"]`).css(newCSS)
  }

  UpdateBlockCache(view.blocks[control.block_id].block.recurrence_id, {
    'dimension': +$('#block-type-edit .dim').val(),
    'start_x': +$('#block-type-edit .x-start').val(),
    'start_y': +$('#block-type-edit .y-start').val(),
    'repeat_x': +$('#block-type-edit .x-repeat').val(),
    'repeat_y': +$('#block-type-edit .y-repeat').val(),
    'dir_x': +$('#block-type-edit .x-dir').val(),
    'dir_y': +$('#block-type-edit .y-dir').val(),
    'translate_object_area': $("#translate-object-area").is(":checked") ? 1 : 0,
    'random_rotation': $("#random-rotation").is(":checked") ? 1 : 0,
    'ground': $('#ground').is(':checked') ? 1 : 0,
    'parent_id': $("#parent-id").html()
  });

  var xhr = new XMLHttpRequest()
  xhr.open('POST', `/update-block/${view.blocks[control.block_id].block.recurrence_id}`)
  xhr.addEventListener('load', function() {
    let res
    try {
      res = JSON.parse(this.response)
      LoadView()
    } catch (err) {
      console.error(err)
    }
  })
  let payload = Object.assign({}, view.blocks[control.block_id].block)
  payload.css = JSON.stringify(payload.css)
  xhr.send(JSON.stringify(
    payload
  ));
}

function SelectParentId() {
  control.selecting_parent_id = true
  $("#select-parent-id").css({
    "background": "turquoise",
    "color": "black"
  })
}

function ChangeBlockPosition() {
  var dragged_id = control.dragged_tile_id.split('::')
  dragged_id[1] = dragged_id[1].split('_').map(Number)
  var block = view.blocks[dragged_id[0]].block
  var x_diff = view.drop_area.start_x - dragged_id[1][1]
  var y_diff = view.drop_area.start_y - dragged_id[1][0]
  var start_y = block.start_y + y_diff
  var start_x = block.start_x + x_diff

  const edits = {
    'start_y': start_y,
    'start_x': start_x
  }

  var xhr = new XMLHttpRequest();
  xhr.open("POST", "/update-block/" + block.recurrence_id)
  xhr.addEventListener('load', function() {
    var res = JSON.parse(this.response);
    if (res.status == 'success') {
      UpdateBlockCache(block.recurrence_id, edits)
      LoadView()
    }
  });
  xhr.send(JSON.stringify({
    ...edits,
    dimension: block.dimension,
    repeat_x: block.repeat_x,
    repeat_y: block.repeat_y,
    dir_x: block.dir_x,
    dir_y: block.dir_y,
    css: JSON.stringify(block.css)
  }))
}

function AddObjectAreas() {
  $('.object-area').remove();
  if (view.saving_object_areas || view.view_block_areas) {
    for (var b of Object.values(view.blocks)) {
      var block = b.block
      var object_area_index = 0
      for (var y = block.start_y; y < block.start_y + (block.dimension * block.repeat_y); y += block.dimension * (block.dir_y / Math.abs(block.dir_y))) {
        for (var x = block.start_x; x < block.start_x + (block.dimension * block.repeat_x); x += block.dimension * (block.dir_x / Math.abs(block.dir_x))) {
          let renderedBlock = Object.assign({}, block);
          renderedBlock.start_x = x
          renderedBlock.start_y = y
          const { top, left, width, height } = GetBlockDimensions(renderedBlock)
          if (view.saving_object_areas || view.view_block_areas) {
            CreateAndAddBlockArea(renderedBlock, top, left, width, height, object_area_index++)
          }
        }
      }
    }
    view.saving_object_areas = false
  }
}

function GetBlockDimensions(block) {
  var css = block.css
  var startTile = document.getElementById(`tile_${block.start_y}-${block.start_x}`);
  var tileWidth = +getComputedStyle(startTile).width.split('px')[0] * block.dimension
  var tileHeight = tileWidth
  return {
    top: startTile.offsetTop,
    left: startTile.offsetLeft,
    width: tileWidth,
    height: tileHeight
  }
}

function calculateAbsoluteOffsets(left, top, width, height, transform, originX = 0, originY = 0, translateObjectArea, units) {
  // Calculate new scaled dimensions
  const scaledWidth = width * transform.scale;
  const scaledHeight = height * transform.scale;

  // Calculate the apparent shift due to scaling
  const offsetLeft = (transform.scale - 1) * width * originX;
  const offsetTop = (transform.scale - 1) * height * originY;

  for (var transformation in transform) {
      debugger
    switch (units[transformation]) {
    case 'px':
      transform[transformation] = transform[transformation]
      break;
    case '%':
      transform[transformation] = transform[transformation] * 0.01 * scaledWidth
      break;
    }
  }

  // Adjust the absolute position
  const newLeft = left - offsetLeft + (translateObjectArea ? transform.translateX : 0)
  const newTop = top - offsetTop + (translateObjectArea ? transform.translateY : 0)

  return { newLeft, newTop };
}

function CreateAndAddBlockArea(block, top, left, width, height, index) {
  var _transform = block.css.transform
  var transform = {
    scale: 1,
    translateX: 0,
    translateY: 0
  };

  var regex = /([a-zA-Z]+)\((-?\d+(\.\d+)?(px|deg|%)?)\)/g;
  var match;
  var unitRegex = /[a-zA-Z%]+/g;
  var units = {}

  while ((match = regex.exec(_transform)) !== null) {
    var unit = match[2].match(unitRegex)
    if (unit) {
      units[match[1]] = unit[0]
    }
    transform[match[1]] = +match[2].replace(unitRegex, '');
  }
  var segment = (width * transform.scale) / 7
  var viewElement = document.getElementById('view')
  for (var object_area of block.object_area) {
    var objectAreaId = `object-area-${block.id}-${object_area[0]}_${object_area[1]}__${index}`;
    var objectArea = document.createElement('div');
    objectArea.classList.add('object-area');
    objectArea.id = objectAreaId;

    
    var { newLeft, newTop } = calculateAbsoluteOffsets(left, top, width, height, transform, 0.5, 0.5, block.translate_object_area, units)
    newTop = newTop + (segment * object_area[0])
    newLeft = newLeft + (segment * object_area[1])

    $(objectArea).css({
      width: segment + 'px',
      height: segment + 'px',
      top: newTop + 'px',
      left: newLeft + 'px'
    });


    if (view.saving_object_areas) {
      view.object_areas.push({ 
        block_id: block.id, 
        id: objectAreaId, 
        top: newTop, 
        left: newLeft, 
        width: segment, 
        height: segment 
      })
    }


    if (view.view_block_areas) {
      viewElement.appendChild(objectArea);
    }
  }
}

function ChangeViewObjectAreas() {
  var doView = $('#view-object-areas').is(':checked')
  view.view_block_areas = doView
  AddObjectAreas()
}

function DeleteBlock() {
  var xhr = new XMLHttpRequest();
  xhr.open("DELETE", "/delete-block/" + view.blocks[control.block_id].block.recurrence_id);
  xhr.addEventListener("load", function() {
    var res = JSON.parse(this.response);
    if (res.status == 'success') {
      DeleteFromBlockCache(view.blocks[control.block_id].block.recurrence_id);
      DeselectBlock()
      LoadView();
    }
  });
  xhr.send();
}

function KeyDown(event) {
  const eventKey = event.key.toLowerCase();

  if (view.sprite.key_values[eventKey]) {
    view.sprite.keys.add(eventKey);
    view.sprite.direction = eventKey.replace('arrow', '');
  }

  if (eventKey === 'shift') {
    view.sprite.shift = true;
  }
}

function KeyUp(event) {
  const eventKey = event.key.toLowerCase();

  if (eventKey === 'shift') {
    view.sprite.shift = false;
  }

  if (view.sprite.keys.has(eventKey)) {
    view.sprite.keys.delete(eventKey);

    const remainingKeys = Array.from(view.sprite.keys);
    if (remainingKeys.length > 0) {
      const lastKey = remainingKeys[remainingKeys.length - 1];
      view.sprite.direction = lastKey.replace('arrow', '');
    }
  }

  view.sprite.rotation[view.sprite.direction].index = 0
}


$( function() {
  console.log('starting...')
  player = {
    name: getCookieValue("name"),
    level: getCookieValue("level"),
    position_x: getCookieValue("position_x"),
    position_y: getCookieValue("position_y"),
    direction: getCookieValue("direction"),
    z_index: getCookieValue("z_index")
  }

  window.view.sprite.position.left = +player.position_x
  window.view.sprite.position.top = +player.position_y
  window.view.sprite.z_index = +player.z_index

  document.querySelector("#log p").innerHTML = Object.keys(player).map(key => `<div><strong>${key}:</strong> <span>${player[key]}</span></div>`).join('')

  var view = document.getElementById('view')
  for (var i = 0; i < 400; i++) {
    var tile = document.createElement('div')
    tile.id = `tile_${ Math.floor(i / 20) }-${ i % 20 }`
    tile.innerHTML = tile.id
    tile.classList.add('tile')
    MakeDroppable(tile)
    view.appendChild(tile)
  }

  var biea = document.getElementById('block-image-edit-area')
  for (var i = 0; i < 49; i++) {
    var object_area_preview = document.createElement('div')
    object_area_preview.classList.add('object-area-preview')
    object_area_preview.id = `object-area-${Math.floor(i / 7)}_${i % 7}`
    biea.appendChild(object_area_preview)
  }

  LoadView()
  LoadImageIds()

  for (var key in events) {
    var split_key = key.split(':')
    document.querySelectorAll(split_key[0]).forEach(element => {
      element.addEventListener(split_key[1], events[key])
    })
  }

  $('#tabs').tabs()


  window.addEventListener('resize', function() {
    LoadView();
  })


  RenderGandalf(window.view.sprite.el)

} )


