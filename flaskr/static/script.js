var events = {
  '#search-pixabay:click': SearchPixabay,
  '#pixabay-results:scroll': ScrollPixabayResults,
  '.pixabay-result:dblclick': SaveImage
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

var searching = false
function SearchPixabay(event, fresh = true) {
  if (searching) return
  searching = true
  var elem = document.getElementById('pixabay-search')
  var results = document.getElementById('pixabay-results')
  if (fresh) {
    results.innerHTML = ''
  }
  var term = elem.value
  var page = elem.dataset.page
  Pixabay(term, page).then(json => {
    console.log(json.hits.length)
    for (var i = 0; i < json.hits.length; i++) {
      var div = document.createElement('div')
      div.classList.add('pixabay-result')
      div.style.background = `url(${json.hits[i].largeImageURL})`
      MakeDraggable(div)
      AddEvents('.pixabay-result', div)
      results.appendChild(div)
    }
    elem.dataset.page = +page + 1
    searching = false
  })
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
  return new Promise(resolve => {
    var url = event.srcElement.style.background.replace('url(', '').replace(')', '')
    var xhr = new XMLHttpRequest()
    xhr.open('POST', '/api/save-image')
    xhr.addEventListener('load', function() {
      var res = JSON.parse(this.response)
      resolve(res)
    })
    xhr.send(url)
  })
}

function MakeDraggable(element) {
  $(element).draggable({
    start: function(event, ui) {
      $('#pixabay-results').css('overflow', 'visible')
    },
    stop: function(event, ui) {
      $('#pixabay-results').css('overflow', 'auto')
    },
    revert: "invalid"
  })
}

function MakeDroppable(element) {
   $(element).droppable({
    drop: function(event, ui) {

    },
    over: function(event, ui) {
      const dimensions = {
        width: $("#drop-dimensions .width").val(),
        height: $("#drop-dimensions .height").val()
      }
      $(event.target).addClass("over")
    },
    out: function(event, ui) {
      $(event.target).removeClass("over")
    }
  })
}

$( function() {
  for (var key in events) {
    var split_key = key.split(':')
    document.querySelectorAll(split_key[0]).forEach(element => {
      element.addEventListener(split_key[1], events[key])
    })
  }

  $('.block').each(function(_, element) {
    MakeDroppable(element)
  })

  $('#tabs').tabs()
} )