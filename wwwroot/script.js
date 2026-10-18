var events = {
  '#log-out:click': Logout,
  '#search-pixabay:click': SearchPixabay,
  '#image-browse-results:scroll': ScrollPixabayResults,
  '.pixabay-result:dblclick': SaveImage,
  '#image-browse-select:change': SelectTag,
  '.tile:click': SelectBlock,
  '.object-area-preview:mousedown': ObjectAreaPreviewMousedown,
  '.object-area-preview:mousemove': ObjectAreaPreviewMousemove,
  '.object-area-preview:mouseup': ObjectAreaPreviewMouseup,
  '#block-css:keyup': ValidateBlockCSS,
  '#apply-block-edits:click': ApplyBlockEdits,
  '#view-object-areas:change': ChangeViewObjectAreas,

  '#view-debug-areas:change': ChangeViewDebugAreas,
  '#delete-block:click': DeleteBlock,
  // '.tile:click': DeselectBlock,
  '#manage-blocks .tbody .tr:mouseover': MouseOverBlockRow,
  '#manage-blocks .tbody .tr:mouseleave': MouseLeaveBlockRow,
  '#manage-blocks .tbody .tr:click': SelectManageBlockRow,
  '#select-parent-id:click': SelectParentId,
  '#select-boundary-tile-ids:click': SelectBoundaryTiles,
  '#view.selecting-boundaries:mousedown': SelectBoundariesMousedown,
  '#view.selecting-boundaries:mousemove': SelectBoundariesMousemove,

  'window:keydown': KeyDown,
  'window:keyup': KeyUp,
  'window:mouseup': MouseUp,

  '#level-name:change': SerializeAndSaveLevelMetadata,
  '#level-name:keyup': SerializeAndSaveLevelMetadata
}

var view = {
  dimension: 20,
  blocks: {},
  $height: $('#view').height(),
  drop_area: {},
  view_block_areas: false,
  view_debug_areas: false,
  object_areas: [],
  wall_bump_sound: new Audio("/wall-bump-sound.mp3"),
  activity: {},
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
    direction: getCookieValue("direction") || 'up',
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
  selecting_parent_id: false,
  selecting_boundaries: false,
  boundary_tile_ids: new Set()
}

var player = {}

Array.prototype.contains = function(str) {
  for (var i = 0; i < this.length; i++) {
    if (this[i] == str) {
      return true
    }
  }
  return false
}

Array.prototype.remove = function(obj) {
  for (var i = 0; i < this.length; i++) {
    if (this[i] == obj) {
      this.splice(i, 1);
      return
    }
  }
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
  xhr.open('POST', '/player/logout')
  xhr.setRequestHeader("Content-Type", "application/json");
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
    if (fresh) elem.dataset.page = 1;
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
  xhr.open('POST', '/image/save')
  xhr.setRequestHeader("Content-Type", "application/json");
  xhr.addEventListener('load', function() {
    let res
    try {
      res = JSON.parse(this.response)
    } catch (err) {
      console.error(err)
    }
    if (res.status == 'success') {
      LoadImageIds();
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
    tag
  }))
}

function MakeDraggable(element) {
  $(element).draggable({
    start: function(event, ui) {
      $('.tile').css('z-index', 9999);
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

      const dimension = Math.abs(Math.floor($(".drop-dimensions input").val()))
      const xRepeat = Math.abs(Math.floor($(".x-repeat").val()))
      const yRepeat = Math.abs(Math.floor($(".y-repeat").val()))
      const xDir = Math.abs(Math.floor($(".x-dir").val()))
      const yDir = Math.abs(Math.floor($(".y-dir").val()))

      var id = event.target.id.replace('tile_', '').split("_")
      id[1] = id[1].split("-").map(Number);

      $('#tile-over-id').html(event.target.id)

      view.drop_area = {
        dimension: control.copy ? view.blocks[control.block_id].block.dimension : dimension,
        repeat_x: xRepeat, 
        repeat_y: yRepeat,
        x_dir: xDir, 
        y_dir: yDir,
        level_grid: id[0],
        start_x: id[1][1],
        start_y: id[1][0]
      }

      for (var y = id[1][0]; y < id[1][0] + (dimension * yRepeat); y += dimension * (yDir / Math.abs(yDir))) {
        for (var x = id[1][1]; x < id[1][1] + (dimension * xRepeat); x += dimension * (xDir / Math.abs(xDir))) {
          for (var i = y; i < y + dimension; i++) {
            for (var j = x; j < x + dimension; j++) {
              if (i < view.dimension && j < view.dimension) {
                const elem = document.getElementById(`tile_${id[0]}_${i}-${j}`);
                $(elem).addClass('over')
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
  const url = '/blocks/save/' + player.level.id + '/' + control.image_id
  xhr.open('POST', url)
  xhr.setRequestHeader("Content-Type", "application/json");
  xhr.addEventListener('load', function() {
    let res
    try {
      res = JSON.parse(this.response)
    } catch (err) {
      console.error(err)
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
  xhr.open("GET", '/blocks/get/' + player.level_id);
  xhr.setRequestHeader("Content-Type", "application/json");
  xhr.addEventListener("load", function() {
    let res
    try {
      res = JSON.parse(this.response)
    } catch (err) {
      console.error(err)
    }
    if (res.status == 'success') {


      control.needsUpdate = false
      var blocks = res.data.map(block => {
        block.object_area = JSON.parse(block.object_area)
        block.css = JSON.parse(block.css)
        if (!view.blocks[block.id]) {
          view.blocks[block.id]  = {
            block: block,
            divs: [],
            children_ids: []
          }
        }
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
                  case 'image_id': return `<div class="td image" style="background-image: url(/image/${block[key]})"></div>`
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
  const viewElement = document.getElementById('view');

  for (const block of blocks) {
    const transform = String(block.css.transform ?? '');
    const scale = Number(
      transform.match(/scale\(\s*([+-]?(?:\d+\.?\d*|\.\d+))/)?.[1] ?? 1
    );

    const dimension = Number(block.dimension);
    const step = dimension * scale;

    const dirX = Number(block.dir_x) < 0 ? -1 : 1;
    const dirY = Number(block.dir_y) < 0 ? -1 : 1;

    const repeatX = Math.max(0, Math.floor(Number(block.repeat_x)));
    const repeatY = Math.max(0, Math.floor(Number(block.repeat_y)));

    const baseX = Number(block.start_x);
    const baseY = Number(block.start_y);

    for (let yi = 0; yi < repeatY; yi++) {
      for (let xi = 0; xi < repeatX; xi++) {
        const x = baseX + xi * step * dirX;
        const y = baseY + yi * step * dirY;

        const renderedBlock = {
          ...block,
          start_x: x,
          start_y: y
        };

        const div = document.createElement('div');

        div.id = `${block.id}::${y}_${x}`;
        div.classList.add('block');

        if (block.id === control.block_id) {
          div.classList.add('editing');
        }

        const css = { ...block.css };

        css.backgroundImage = `url(/image/${block.image_id})`;
        delete css.zIndex;

        if (block.random_rotation > 0) {
          const rotation = Math.floor(Math.random() * 4) * 90;

          css.transform = String(css.transform ?? '')
            .replace(/rotate\([^)]*\)/g, '')
            .trim();

          css.transform = `${css.transform} rotate(${rotation}deg)`.trim();
        }

        // Use the original tile as the coordinate reference.
        const tileY = Math.max(0, Math.min(19, Math.floor(y)));
        const tileX = Math.max(0, Math.min(19, Math.floor(x)));

        const startTile = document.getElementById(
          `tile_${block.level_grid}_${tileY}-${tileX}`
        );

        if (!startTile) continue;

        const tileSize = startTile.getBoundingClientRect().width;
        const blockSize = tileSize * dimension;

        // Preserve fractional tile positions.
        const top = startTile.offsetTop + (y - tileY) * tileSize;
        const left = startTile.offsetLeft + (x - tileX) * tileSize;

        $(div).css({
          ...css,
          width: `${blockSize}px`,
          height: `${blockSize}px`,
          top: `${top}px`,
          left: `${left}px`
        });

        div.dataset.id = block.id;
        div.dataset.recurrence_id = block.recurrence_id;

        if (block.ground) {
          div.classList.add('ground');
        }

        viewElement.appendChild(div);

        MakeDraggable(div);

        view.blocks[block.id].divs.push(div);
      }
    }
  }

  AddObjectAreas();
  AdjustEditBlockImage();
  AddEvents();
}


function FinalizeBlocks() {

  // STEP 1: Calculate the z-index of every block.
  for (const id in view.blocks) {

    // Retrieve the block's data and rendered HTML elements.
    const { block, divs } = view.blocks[id];

    // Ground blocks (floor textures, etc.) always stay behind everything.
    if (block.ground) {
      for (const div of divs) {
        $(div).css('z-index', 1);
      }
      continue;
    }

    // A block can have multiple rendered instances because of repeat_x/repeat_y.
    // Calculate the depth of each rendered instance separately.
    divs.forEach((div, index) => {

      // Find the object areas belonging to this specific block instance.
      // CreateAndAddBlockArea() assigns each instance an "__index" suffix.
      const areas = view.object_areas.filter(area =>
        area.block_id === id &&
        area.id.endsWith(`__${index}`)
      );

      // Determine the bottom edge of the actual rendered block.
      // This is our fallback if the block has no object areas.
      const divBottom = div.offsetTop + div.offsetHeight;

      // If object areas exist, use the LOWEST (frontmost) edge
      // of the object's physical footprint.
      //
      // Because Y increases downward:
      // Math.max() gives us the frontmost edge.
      //
      // Otherwise, use the bottom of the rendered block.
      const depthY = areas.length
        ? Math.max(...areas.map(area => area.top + area.height))
        : divBottom;

      // Assign depth as the z-index.
      // Objects farther down the screen appear in front.
      $(div).css('z-index', Math.floor(depthY));

      if (view.view_debug_areas && div.children.length == 0) {
        div.innerHTML = `<div style="opacity:0.5;font-size:.25rem;background:white;display:flex;align-items:center;justify-content:center">
          depth y: ${depthY}
          z: ${Math.floor(depthY)}
      </div>`
      }

    });
  }


  // STEP 2: Position child blocks immediately above their parents.
  //
  // For example, a cushion belonging to a couch should render
  // above the couch rather than being independently depth-sorted.
  for (const id in view.blocks) {

    const { block, divs } = view.blocks[id];

    // Ground blocks have already been handled.
    if (block.ground) continue;

    // Skip blocks that have no parent.
    if (!block.parent_id) continue;

    // Look up the parent block.
    const parent = view.blocks[block.parent_id];

    // Ignore missing parents.
    if (!parent || !parent.divs.length) continue;

    // Find the highest z-index among the parent's rendered instances.
    const parentDepth = Math.max(
      ...parent.divs.map(div => Number($(div).css('z-index')))
    );

    // Place the child just above its parent.
    for (const div of divs) {
      $(div).css('z-index', parentDepth + 1);
    }
  }
}


function LoadTiles(level_grid) {
  var view = document.getElementById('view')
  for (var i = 0; i < 400; i++) {
    var tile = document.createElement('div')
    tile.id = `tile_${level_grid}_${ Math.floor(i / 20) }-${ i % 20 }`
    tile.classList.add('tile')
    tile.classList.add('dev')
    MakeDroppable(tile)
    view.appendChild(tile)
  }
}

function LoadView() {
  view.block_areas = [];
  for (var id in view.blocks) {
    view.blocks[id].divs = []
  }
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
  AddObjectAreas()
  LoadSprite()
  view.$height = $("#view").height()
}

function LoadSprite() {
  view.sprite.direction = player.direction
  view.sprite.el = document.createElement("div")
  view.sprite.el.id = "gandalf"
  view.sprite.el.classList.add('sprite')
  document.querySelector('#view').appendChild(view.sprite.el)
  RenderGandalf()
}



function LoadImageIds() {
  var xhr = new XMLHttpRequest()
  xhr.open('GET', '/image/ids')
  xhr.setRequestHeader("Content-Type", "application/json");
  xhr.addEventListener('load', function() {
    let res
    try {
      res = JSON.parse(this.response)
    } catch (err) {
      console.error(err)
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
        control.image_urls[res.data[i].tag].push(`/image/${res.data[i].id}`)
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
  const container = document.getElementById('block-image-edit-area');
  const image = document.getElementById('block-image');

  const width = container.clientWidth;
  const height = container.clientHeight;

  $('#block-image-container, #block-image').css({
    width: width + 'px',
    height: height + 'px'
  });
}

function SelectBlock(event) {
  console.log('SelectBlock', event.srcElement)
  var id;

  if (event.srcElement.id) {
    id = event.srcElement.id.split("__")[1].replace('-', '_');
  } else if (event.srcElement.dataset) {
    id = event.srcElement.dataset.id;
  }

  let selectedBlocks = [];


  for (let child of document.getElementById('view').children) {
    if (child.id.endsWith("::" + id) || child.id.startsWith(id)) {
      selectedBlocks.push(child);
    }
  }

  if (!selectedBlocks.length) return;


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



  control.block_id = /*just 0 for now*/selectedBlocks[0].id.split("::")[0];



  view.blocks[control.block_id].divs.forEach(div => {
    $(div).addClass('editing')
  });

  const block = view.blocks[control.block_id].block;


  $("#block-id").html(block.id)
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

  $('a[href="#edit-block"]').click();

  requestAnimationFrame(() => {
    AdjustEditBlockImage();
  });


  var css = Object.assign({}, view.blocks[control.block_id].block.css);

  $('#block-css').val(JSON.stringify(css, null, 1))

  css.backgroundImage = `url(/image/${view.blocks[control.block_id].block.image_id})`;
  css.transform = ''//css.transform.replace(/scale\(\d+(\.\d+)?\)/, '')

  if (css.transform) {
    css.transform = css.transform.replace(/scale\(.+\)/, '')
  }
  // console.log('css for editor', css)
  $('#block-image').css(css)
  $('#block-type-edit .drop-dimensions input').val(view.blocks[control.block_id].block.dimension)

  let copyBlock = document.getElementById('copy-block')
  $(copyBlock).css('background-image', css.backgroundImage);
  copyBlock.dataset.copy = true
  copyBlock.dataset.id = block.id
  MakeDraggable(copyBlock)

  ValidateBlockCSS()
  AdjustEditBlockImage()


  $('.z-index').val(css.zIndex)

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

function MouseOverBlockRow(event) {
  var tr = event.srcElement;
  while (tr && !tr.classList.contains('tr')) {
    tr = tr.parentElement;
  }
  var block_id = tr.children[0].innerHTML
  $('.block[data-id="' + block_id + '"]').addClass('hovered')
}

function MouseLeaveBlockRow(event) {
  var tr = event.srcElement;
  while (tr && !tr.classList.contains('tr')) {
    tr = tr.parentElement;
  }
  var block_id = tr.children[0].innerHTML
  $('.block[data-id="' + block_id + '"]').removeClass('hovered')
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
  view.activity.click = true;
  event.preventDefault()
  var selected = $(event.target).hasClass("selected");
  control.object_area_preview.select = !selected;
  control.object_area_preview.active = true

  if (control.object_area_preview.select) {
    $(event.target).addClass('selected')
  } else {
    $(event.target).removeClass('selected')
  }
  
}

function ObjectAreaPreviewMousemove(event) {
  view.activity.mousemove = true;
  setTimeout(() => {
    view.activity.mousemove = false;
  }, 700);
  if (control.object_area_preview.active) {
    if (control.object_area_preview.select) {
      $(event.target).addClass('selected')
    } else {
      $(event.target).removeClass('selected')
    }
  }
}

function ObjectAreaPreviewMouseup(event) {
  delete view.activity.click;
  control.object_area_preview.active = false
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
  xhr.open('POST', `/blocks/update-object-area/${view.blocks[control.block_id].block.recurrence_id}`)
  xhr.setRequestHeader("Content-Type", "application/json");
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
    css.transform = css.transform.replace(/scale\(.+\)/, '')

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
    'parent_id': $("#parent-id").html(),
    'level_id': view.blocks[control.block_id].block.level_id
  });

  var xhr = new XMLHttpRequest()
  xhr.open('POST', `/blocks/update/${view.blocks[control.block_id].block.recurrence_id}`)
  xhr.setRequestHeader("Content-Type", "application/json");
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
  payload.object_area = JSON.stringify(payload.object_area);
  if (payload.parent_id.length !== 36) {
    payload.parent_id = null;
  }
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

function SelectBoundaryTiles() {
  if (!control.selecting_boundaries) {
    view.view_block_areas = true
    $("#view-object-areas").prop("checked", true)
    AddObjectAreas()
    $('#view').addClass('selecting-boundaries')
    $("#select-boundary-tile-ids").addClass('active')
    $('.tile').css('z-index', 9999);
  } else {
    view.view_block_areas = false
    $("#view-object-areas").prop("checked", false)
    AddObjectAreas()
    $('#view').removeClass('selecting-boundaries')
    $("#select-boundary-tile-ids").removeClass('active')
    $('.tile').css('z-index', 0);
  }
  AddEvents()
  control.selecting_boundaries = !control.selecting_boundaries
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
  xhr.open("POST", "/blocks/update/" + block.recurrence_id)
  xhr.setRequestHeader("Content-Type", "application/json");
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

function isOverlappedByGround(element) {
    const rect = element.getBoundingClientRect();
    const centerX = (rect.left + rect.right) / 2;
    const centerY = (rect.top + rect.bottom) / 2;
    const topElement = document.elementFromPoint(centerX, centerY);
    return topElement.classList.contains('ground');
}

function AddObjectAreas() {
  view.object_areas = []
  $('.object-area').remove();
  if (player.level && player.level.boundary_tile_ids) {
    for (var id of player.level.boundary_tile_ids) {
      AddObjectAreaFromBoundaryId(id)
    }
  }

  for (var b of Object.values(view.blocks)) {
    var block = b.block
    var object_area_index = 0
    for (var y = block.start_y; y < block.start_y + (block.dimension * block.repeat_y); y += block.dimension * (block.dir_y / Math.abs(block.dir_y))) {
      for (var x = block.start_x; x < block.start_x + (block.dimension * block.repeat_x); x += block.dimension * (block.dir_x / Math.abs(block.dir_x))) {
        let renderedBlock = Object.assign({}, block);
        renderedBlock.start_x = x
        renderedBlock.start_y = y
        

        // still need to accomodate for expanding to adjacent grids
        try {
          const { top, left, width, height } = GetBlockDimensions(renderedBlock)

          CreateAndAddBlockArea(renderedBlock, top, left, width, height, object_area_index++)
        } catch (err) {
          continue
        }
      }
    }
  }
  FinalizeBlocks()
}

function GetBlockDimensions(block) {
  var startTile = document.getElementById(`tile_${block.level_grid}_${block.start_y}-${block.start_x}`);


  // still need to accomodate for expanding to adjacent grids
  if (!startTile) return null;


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
    switch (units[transformation]) {
    case 'px':
      transform[transformation] = transform[transformation]
      break;
    case '%':
      transform[transformation] = transform[transformation] * 0.1 * scaledWidth
      break;
    }
  }

  // Adjust the absolute position
  const newLeft = left - offsetLeft + (translateObjectArea ? transform.translateX : 0)
  const newTop = top - offsetTop + (translateObjectArea ? transform.translateY : 0)

  return { newLeft, newTop };
}

function CreateAndAddBlockArea(block, top, left, width, height, index) {
  var _transform = block.css.transform ?? 1;
  var transform = {
    scale: 1,
    scaleX: 1,
    scaleY: 1,
    translateX: 0,
    translateY: 0,
    rotate: 0,
    skewX: 0,
    skewY: 0
  };

  if (typeof _transform === "number" || !isNaN(Number(_transform))) {
    transform.scale = Number(_transform);
  } else {
    const regex = /([a-zA-Z]+)\(([^)]*)\)/g;

    for (const [, name, value] of _transform.matchAll(regex)) {
      const values = value.split(/[,\s]+/).map(parseFloat);

      if (name === "translate") {
        transform.translateX = values[0] ?? 0;
        transform.translateY = values[1] ?? 0;
      } else if (name === "scale") {
        transform.scale = values[0] ?? 1;
        if (values.length > 1) {
          transform.scaleX = values[0];
          transform.scaleY = values[1];
        }
      } else if (name in transform) {
        transform[name] = values[0];
      }
    }
  }
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


    view.object_areas.push({ 
      block_id: block.id, 
      id: objectAreaId, 
      top: newTop, 
      left: newLeft, 
      width: segment, 
      height: segment 
    });


    if (view.view_block_areas) {
      viewElement.appendChild(objectArea);
    }
  }
}

function ChangeViewObjectAreas() {
  var doView = $('#view-object-areas').is(':checked')
  view.view_block_areas = doView;
  AddObjectAreas()
}

function ChangeViewDebugAreas() {
  var doView = $('#view-debug-areas').is(':checked')
  view.view_debug_areas = doView;
}

function DeleteBlock() {
  var xhr = new XMLHttpRequest();
  xhr.open("DELETE", "/blocks/delete/" + view.blocks[control.block_id].block.recurrence_id);
  xhr.setRequestHeader("Content-Type", "application/json");
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

function AddObjectAreaFromBoundaryId(boundary_tile_id) {
  var $tile = document.getElementById(boundary_tile_id)
  var objectArea = document.createElement('div');

  objectArea.classList.add('object-area');
  objectArea.id = 'object_area_' + boundary_tile_id;
  const width = $($tile).width()
  const height = $($tile).height()
  const top = $($tile).offset().top
  const left = $($tile).offset().left

  $(objectArea).css({
    width: width + 'px',
    height: height + 'px',
    top: top + 'px',
    left: left + 'px'
  });
  
  if (view.view_block_areas) {
    document.getElementById("view").appendChild(objectArea)
  }

  view.object_areas.push({ 
    block_id: null, 
    id: boundary_tile_id, 
    top, 
    left, 
    width, 
    height 
  });
}

function SelectBoundariesMousedown(event) {
  view.activity.click = true;
  if (!event.target.classList.contains("tile")) return
  
  var id = event.target.id;
  var objectAreaId = `object_area_${id}`;
  
  if (!player.level.boundary_tile_ids.contains(id)) {
    player.level.boundary_tile_ids.push(id);
    player.level.boundary_tile_ids = Array.from(new Set(player.level.boundary_tile_ids));
    AddObjectAreaFromBoundaryId(id, true)
    control.select_boundaries_mousedown = true
  } else {
    player.level.boundary_tile_ids.splice(player.level.boundary_tile_ids.indexOf(id), 1);
    document.getElementById(objectAreaId).remove()
    control.removing_boundary_tile = true
    control.select_boundaries_mousedown = false
  }


  $('#boundary-tile-ids').html(JSON.stringify(player.level.boundary_tile_ids))
}

function SelectBoundariesMousemove(event) {
  view.activity.mousemove = true;
  setTimeout(() => {
    view.activity.mousemove = false;
  }, 700);
  if (!event.target.classList.contains("tile") || control.removing_boundary_tile) return
  if (control.select_boundaries_mousedown) {
    var id = event.target.id;
    control.boundary_tile_ids.add(id)
    player.level.boundary_tile_ids = Array.from(control.boundary_tile_ids)

    let ids = JSON.stringify(Array.from(control.boundary_tile_ids))

    $('#boundary-tile-ids').html(ids)
    
    var objectAreaId = `object-area_fromtile-${id}`;
  
    if (!document.getElementById(objectAreaId)) {
      AddObjectAreaFromBoundaryId(id, true)
    }
  }
}

function KeyDown(event) {
  view.activity[event.key] = true;
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
  delete view.activity[event.key];
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

function MouseUp(event) {
  delete view.activity.click;
  if (control.select_boundaries_mousedown || control.removing_boundary_tile) {
    control.select_boundaries_mousedown = false;
    var xhr = new XMLHttpRequest();
    xhr.open("POST", "/level/" + player.level.id)
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.addEventListener("load", function() {
      var res = JSON.parse(this.response);
      if (res.status == 'success') {
        LoadView()
      }
    });
    var payload = Object.assign({}, player.level);
    payload.boundary_tile_ids = JSON.stringify(player.level.boundary_tile_ids)
    xhr.send(JSON.stringify(payload))
  }
  control.removing_boundary_tile = false
}

function LoadPlayerFromCache() {
  // load player from cache
  player = {
    name: getCookieValue("name"),
    level_id: getCookieValue("level_id"),
    position_x: getCookieValue("position_x"),
    position_y: getCookieValue("position_y"),
    direction: getCookieValue("direction"),
    z_index: getCookieValue("z_index"),
    top_left_tile: getCookieValue("top_left_tile"),
    level_grid: getCookieValue("level_grid")
  }
  window.view.sprite.position.left = +player.position_x
  window.view.sprite.position.top = +player.position_y
  window.view.sprite.z_index = +player.z_index

  document.querySelector("#log p").innerHTML = `
    <span>${player.level}</span>
  `
}

function LoadPlayerPostInitalFromServer(res) {
  player.level = res.data;
      
  try {
    player.level.boundary_tile_ids = JSON.parse(player.level.boundary_tile_ids)
    for (var id of player.level.boundary_tile_ids) {
      control.boundary_tile_ids.add(id)
    }
    $('#level-name').val(res.data.name);
    $('#boundary-tile-ids').html(JSON.stringify(Array.from(control.boundary_tile_ids)));
  } catch (NoTilesYet) {}
}

function LoadLevel(res) {
  LoadPlayerPostInitalFromServer(res)
  LoadTiles(player.level_grid)

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
}

function LoadData() {
  // player
  LoadPlayerFromCache();

  // level
  var xhr = new XMLHttpRequest();
  xhr.open("GET", "/level/" + player.level_id);
  xhr.setRequestHeader("Content-Type", "application/json");
  xhr.addEventListener("load", function() {
    var res = JSON.parse(this.response)

    if (res.status == 'success') {
      LoadLevel(res);
    }
  });
  xhr.send();
}

$( function() {
  console.log('starting...')

  
  LoadData();

} )




function SerializeAndSaveLevelMetadata() {
  var xhr = new XMLHttpRequest();
  xhr.open('POST', '/level/' + player.level.id);
  xhr.setRequestHeader("content-type", "application/json");
  xhr.addEventListener("load", function() {
    var res = JSON.parse(this.response);
    console.log("saved level", res);
  });
  var levelRequest = player.level;
  levelRequest.name = document.getElementById('level-name').value; // vadefault
  levelRequest.boundary_tile_ids = JSON.stringify(levelRequest.boundary_tile_ids);
  xhr.send(JSON.stringify(levelRequest));
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
  const rotation = view.sprite.rotation[spriteDirection || "up"];
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
    left: view.sprite.position.left * view.$height
  };

 

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
        // $('#' + object_area.id).css('background', 'rgba(255,0,0,0.5)')
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

  switch (view.sprite.direction) {
  default:
    css.zIndex = Math.floor(spriteBottom)
    break;
  }

  $(view.sprite.el).css(css);
  $(view.sprite.el).html(css.zIndex)
  $(view.sprite.el)[0].style.zIndex = css.zIndex


  var gandalfMetadata = document.getElementById('gandalf-metadata')
  if (gandalfMetadata) {
    gandalfMetadata.remove()
  }
  if (view.view_debug_areas) {
    gandalfMetadata = document.createElement('pre')
    gandalfMetadata.id = 'gandalf-metadata'
    gandalfMetadata.style.top = spriteBottom + 'px'
    gandalfMetadata.style.left = spriteLeft + 'px'
    gandalfMetadata.innerHTML = `
      ${JSON.stringify(css, null, 2)}

    `
    document.getElementById('view').appendChild(gandalfMetadata)
  }

  $("#object-areas-count").html(view.object_areas.length)


  cancelAnimationFrame(view.sprite.frame)
  window.requestAnimationFrame(RenderGandalf);
}