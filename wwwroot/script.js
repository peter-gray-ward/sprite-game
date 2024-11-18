var events = {
  '#log-in:click': Login,
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

function Login() {
  var xhr = new XMLHttpRequest()
  xhr.open('POST', '/auth/login')
  xhr.addEventListener('load', function() {
    var res = JSON.parse(this.response)
    if (res.status == 'success') {
      location.reload()
    }
  })
  xhr.send(JSON.stringify({
    username: $('#username').val(),
    password: $('#password').val()
  }))
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
function SearchPixabay(event, fresh = true) {
  if (searching) return
  searching = true
  try {
    var elem = document.getElementById('pixabay-search')
    var results = document.getElementById('pixabay-results')
    if (fresh) {
      results.innerHTML = ''
    }
    var term = elem.value
    var page = elem.dataset.page
    Pixabay(term, page).then(json => {
      for (var i = 0; i < json.hits.length; i++) {
        AppendImageDiv(results, json.hits[i].largeImageURL, term, '.pixabay-result')
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
      $('#pixabay-results').css('overflow', 'visible')
    },
    stop: function(event, ui) {
      $(this).removeClass('dragging')
      $('#pixabay-results').css('overflow', 'auto')
      $('.block').removeClass('over')
    },
    revert: true
  })
}

function MakeDroppable(element) {
   $(element).droppable({
    drop: function(event, ui) {

    },
    over: function(event, ui) {
      $('.block').removeClass('over')
      const dimensions = {
        width: Math.floor($("#drop-dimensions .width").val()),
        height: Math.floor($("#drop-dimensions .height").val())
      }
      var id = event.target.id.replace('block_', '').split('-').map(Number)
      for (var i = id[0]; i < id[0] + dimensions.height; i++) {
        for (var j = id[1]; j < id[1] + dimensions.width; j++) {
          if (i < view.dimension && j < view.dimension) {
            $(`#block_${i}-${j}`).addClass('over')
          }
        }
      }
    }
  })
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
  for (var image_url of control.image_urls[tag]) {
    console.log(image_url)
    AppendImageDiv(results, image_url, tag, '.image')
  }
}

$( function() {
  console.log('starting...')
  var view = document.getElementById('view')
  for (var i = 0; i < 400; i++) {
    var block = document.createElement('div')
    block.id = `block_${ i / 20 }-${ i % 20 }`
    block.classList.add('block')
    MakeDroppable(block)
    view.appendChild(block)
  }

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
} )